using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;
using System.Net.Sockets;

namespace AuthService.Infrastructure.Messaging
{
    /// <summary>
    /// Manages a resilient connection to RabbitMQ, with automatic reconnection and event handling.
    /// </summary>
    public class RabbitConnection : IRabbitConnection
    {
        private readonly ILogger<RabbitConnection> _logger;
        private readonly IConnectionFactory _connectionFactory;
        private readonly CancellationToken _appStopping;
        private IConnection? _connection;
        private bool _disposed;

        private readonly SemaphoreSlim _connectionLock = new(1, 1);

        // Polly policies
        private readonly ResiliencePipeline _resiliencePipeline;

        public RabbitConnection(
            IConnectionFactory connectionFactory,
            ILogger<RabbitConnection> logger,
            IHostApplicationLifetime appLifetime)
        {
            _connectionFactory = connectionFactory;
            _logger = logger;
            _appStopping = appLifetime.ApplicationStopping;

            var retryOptions = new RetryStrategyOptions
            {
                MaxRetryAttempts = 5,
                DelayGenerator = args =>
                {
                    var exponential = Math.Pow(2, args.AttemptNumber);
                    var jitterMs = Random.Shared.Next(0, 1000);

                    return new ValueTask<TimeSpan?>(
                        TimeSpan.FromSeconds(exponential) + TimeSpan.FromMilliseconds(jitterMs));
                },
                ShouldHandle = args =>
                    ValueTask.FromResult(
                        args.Outcome.Exception is SocketException ||
                        args.Outcome.Exception is BrokerUnreachableException
                    ),
                OnRetry = args =>
                {
                    _logger.LogWarning(
                        "[Retry {AttemptNumber}] {ExceptionMessage}",
                        args.AttemptNumber,
                        args.Outcome.Exception?.Message);
                    return default;
                }
            };

            // The circuit breaker will open if 50% of the last 3 attempts fail,
            // and will stay open for 30 seconds before trying again.
            var circuitBreakerOptions = new CircuitBreakerStrategyOptions
            {
                FailureRatio = 0.5,
                MinimumThroughput = 3,
                BreakDuration = TimeSpan.FromSeconds(30),
                ShouldHandle = args =>
                    ValueTask.FromResult(
                        args.Outcome.Exception is SocketException ||
                        args.Outcome.Exception is BrokerUnreachableException
                    ),
                OnOpened = args =>
                {
                    _logger.LogWarning(
                        "[Circuit OPEN] Break duration: {BreakDurationSeconds}s",
                        args.BreakDuration.TotalSeconds);
                    return default;
                },
                OnClosed = args =>
                {
                    _logger.LogWarning("[Circuit CLOSED]");
                    return default;
                },
                OnHalfOpened = args =>
                {
                    _logger.LogWarning("[Circuit HALF-OPEN]");
                    return default;
                }
            };

            _resiliencePipeline = new ResiliencePipelineBuilder()
                .AddRetry(retryOptions)
                .AddCircuitBreaker(circuitBreakerOptions)
                .Build();
        }

        public bool IsConnected =>
            _connection != null && _connection.IsOpen && !_disposed;

        /// <summary>
        /// Attempts to establish a connection to RabbitMQ, with retries and circuit breaker
        /// handling, and supports cancellation.
        /// </summary>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>True if connection is established, false otherwise.</returns>
        public async Task<bool> TryConnectAsync(CancellationToken cancellationToken = default)
        {
            if (_disposed) return false;

            if (IsConnected) return true;

            // [FIX 1] Link the incoming token with the app stopping token so any shutdown
            // cancels an in-progress connection attempt immediately.
            using var linkedCts = CancellationTokenSource
                .CreateLinkedTokenSource(cancellationToken, _appStopping);

            await _connectionLock.WaitAsync(linkedCts.Token);

            try
            {
                if (IsConnected) return true;

                return await _resiliencePipeline.ExecuteAsync(async token =>
                {
                    try
                    {
                        var connection = await _connectionFactory.CreateConnectionAsync(token);

                        if (connection is null || !connection.IsOpen)
                        {
                            _logger.LogWarning("Connection created but not open");
                            return false;
                        }

                        // [FIX 3] Unsubscribe events before disposing the old connection
                        // to prevent stale callbacks firing after replacement.
                        if (_connection != null)
                        {
                            try
                            {
                                _connection.ConnectionShutdownAsync -= OnConnectionShutdownAsync;
                                _connection.CallbackExceptionAsync -= OnCallbackExceptionAsync;
                                _connection.ConnectionBlockedAsync -= OnConnectionBlockedAsync;
                                _connection.Dispose();
                            }
                            catch { /* swallow */ }
                        }

                        _connection = connection;

                        _connection.ConnectionShutdownAsync += OnConnectionShutdownAsync;
                        _connection.CallbackExceptionAsync += OnCallbackExceptionAsync;
                        _connection.ConnectionBlockedAsync += OnConnectionBlockedAsync;

                        _logger.LogInformation("RabbitMQ connected successfully");

                        return true;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to connect to RabbitMQ");
                        throw;
                    }

                }, linkedCts.Token);
            }
            catch (BrokenCircuitException)
            {
                _logger.LogWarning("Circuit is open, skipping connection attempt");
                return false;
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("Connection attempt cancelled");
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Error connecting to RabbitMQ: {ExceptionMessage}", ex.Message);
                return false;
            }
            finally
            {
                _connectionLock.Release();
            }
        }

        /// <summary>
        /// Creates a new channel on the existing RabbitMQ connection. If the connection is not
        /// established, it will attempt to connect first.
        /// </summary>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param> 
        /// <returns>A task containing the created channel.</returns>
        /// <exception cref="InvalidOperationException"></exception>
        public async Task<IChannel> CreateChannelAsync(CancellationToken cancellationToken = default) 
        {
            if (!IsConnected)
            {
                var connected = await TryConnectAsync(cancellationToken);

                if (!connected)
                    throw new InvalidOperationException("Failed to establish a connection with RabbitMQ");
            }

            return await _connection!.CreateChannelAsync(cancellationToken: cancellationToken);
        }

        /// <summary>
        /// Handles the ConnectionShutdown event. Reconnects when the shutdown was initiated by
        /// the peer or the library (not by the application itself).
        /// </summary>
        private async Task OnConnectionShutdownAsync(object sender, ShutdownEventArgs e)
        {
            if (_disposed) return;

            _logger.LogWarning(
                "RabbitMQ connection shutdown. Initiator: {Initiator}, Reason: {ReplyText}",
                e.Initiator,
                e.ReplyText);

            if (e.Initiator == ShutdownInitiator.Peer ||
                e.Initiator == ShutdownInitiator.Library)
            {
                await SafeReconnectAsync("Shutdown");
            }
        }

        /// <summary>
        /// Handles the CallbackException event. Reconnects only on network-related errors.
        /// </summary>
        private async Task OnCallbackExceptionAsync(object sender, CallbackExceptionEventArgs e)
        {
            if (_disposed) return;

            _logger.LogWarning(
                "RabbitMQ callback exception: {ExceptionMessage}",  
                e.Exception.Message);

            if (e.Exception is SocketException)
            {
                await SafeReconnectAsync("CallbackException");
            }
        }

        /// <summary>
        /// Handles the ConnectionBlocked event. Logs the reason but does not reconnect,
        /// as the server may recover on its own under resource pressure.
        /// </summary>
        private Task OnConnectionBlockedAsync(object sender, ConnectionBlockedEventArgs e)
        {
            _logger.LogWarning(
                "RabbitMQ connection blocked: {Reason}", 
                e.Reason);

            return Task.CompletedTask;
        }

        /// <summary>
        /// Safely attempts to reconnect, respecting application shutdown state.
        /// </summary>
        private async Task SafeReconnectAsync(string reason)
        {
            if (_disposed) return;

            _logger.LogWarning(
                "Attempting to reconnect to RabbitMQ. Reason: {Reason}",
                reason);

            //  Pass the app stopping token so reconnection is cancelled on shutdown
            await TryConnectAsync(_appStopping);
        }

        /// <summary>
        /// Disposes the RabbitConnection, unsubscribing from events and closing the connection.
        /// This method is idempotent.
        /// </summary>
        public void Dispose()
        {
            if (_disposed) return;

            _disposed = true;

            // Always unsubscribe before disposing to avoid callbacks firing
            // on a half-torn-down object.
            if (_connection != null)
            {
                try
                {
                    _connection.ConnectionShutdownAsync -= OnConnectionShutdownAsync;
                    _connection.CallbackExceptionAsync -= OnCallbackExceptionAsync;
                    _connection.ConnectionBlockedAsync -= OnConnectionBlockedAsync;
                }
                catch { /* swallow */ }

                try
                {
                    _connection.Dispose();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error closing RabbitMQ connection");
                }
            }
        }
    }
}