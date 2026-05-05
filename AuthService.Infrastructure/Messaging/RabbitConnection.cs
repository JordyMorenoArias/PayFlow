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
        private IConnection? _connection;
        private bool _disposed;

        private readonly SemaphoreSlim _connectionLock = new(1, 1);

        // Polly policies
        private readonly ResiliencePipeline _resiliencePipeline;

        public RabbitConnection(IConnectionFactory connectionFactory, ILogger<RabbitConnection> logger)
        {
            _connectionFactory = connectionFactory;
            _logger = logger;

            var jitter = new Random();

            // Polly configuration: Exponential backoff with jitter for retries, and a circuit breaker to prevent overwhelming the server
            var retryOptions = new RetryStrategyOptions
            {
                MaxRetryAttempts = 5,
                DelayGenerator = args =>
                {
                    var exponential = Math.Pow(2, args.AttemptNumber);
                    var jitterMs = jitter.Next(0, 1000);

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
                    _logger.LogWarning("[Retry {AttemptNumber}] {ExceptionMessage}", args.AttemptNumber, args.Outcome.Exception?.Message);
                    return default;
                }
            };

            // The circuit breaker will open if 50% of the last 3 attempts fail, and will stay open for 30 seconds before trying again
            var circuitBreakerOptions = new CircuitBreakerStrategyOptions
            {
                FailureRatio = 0.5, // 50% failures
                MinimumThroughput = 3,
                BreakDuration = TimeSpan.FromSeconds(30),
                ShouldHandle = args =>
                    ValueTask.FromResult(
                        args.Outcome.Exception is SocketException ||
                        args.Outcome.Exception is BrokerUnreachableException
                    ),
                OnOpened = args =>
                {
                    _logger.LogWarning("[Circuit OPEN] {BreakDuration}s", args.BreakDuration.TotalSeconds);
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
        /// Attempts to establish a connection to RabbitMQ, with retries and circuit breaker handling, and supports cancellation.
        /// </summary>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns> True if connection is established, false otherwise.</returns>
        public async Task<bool> TryConnectAsync(CancellationToken cancellationToken = default)
        {
            if (_disposed) return false;

            if (IsConnected)
                return true;

            await _connectionLock.WaitAsync(cancellationToken);

            try
            {
                if (IsConnected)
                    return true;

                return await _resiliencePipeline.ExecuteAsync(async token =>
                {
                    try
                    {
                        var connection = await _connectionFactory.CreateConnectionAsync(token);

                        // Extra validation 
                        if (connection is null || !connection.IsOpen)
                        {
                            _logger.LogWarning("Connection created but not open");
                            return false;
                        }

                        // cleaning up old connection if exists
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

                        // subscribe to events
                        _connection.ConnectionShutdownAsync += OnConnectionShutdownAsync;
                        _connection.CallbackExceptionAsync += OnCallbackExceptionAsync;
                        _connection.ConnectionBlockedAsync += OnConnectionBlockedAsync;

                        _logger.LogWarning("RabbitMQ connected");

                        return true;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to connect to RabbitMQ");
                        throw; // important to rethrow so Polly can handle it
                    }

                }, cancellationToken);
            }
            catch (BrokenCircuitException)
            {
                _logger.LogWarning("Circuit is open, skipping connection attempt"); 
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Error connecting: {ex.Message}");
                return false;
            }
            finally
            {
                _connectionLock.Release();
            }
        }

        /// <summary>
        /// Creates a new channel on the existing RabbitMQ connection. If the connection is not established, it will attempt to connect first.
        /// </summary>
        /// <returns> A task that represents the asynchronous operation, containing the created channel.</returns>
        /// <exception cref="InvalidOperationException"></exception>
        public async Task<IChannel> CreateChannelAsync()
        {
            if (!IsConnected)
            {
                var connected = await TryConnectAsync();

                if (!connected)
                    throw new InvalidOperationException("Failed to establish a connection with RabbitMQ");
            }

            return await _connection!.CreateChannelAsync();
        }

        /// <summary>
        /// Handles the ConnectionShutdown event from RabbitMQ. If the shutdown was initiated by the peer or the library, it will attempt to reconnect.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// <returns> A task that represents the asynchronous operation.</returns>
        private async Task OnConnectionShutdownAsync(object sender, ShutdownEventArgs e)
        {
            if (_disposed) return;

            _logger.LogWarning($"Shutdown: {e.ReplyText}");

            // only attempt to reconnect if shutdown was initiated by peer or library
            if (e.Initiator == ShutdownInitiator.Peer ||
                e.Initiator == ShutdownInitiator.Library)
            {
                await SafeReconnectAsync("Shutdown");
            }
        }

        /// <summary>
        /// Handles the CallbackException event from RabbitMQ. If the exception is a network error, it will attempt to reconnect.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// <returns> A task that represents the asynchronous operation.</returns>
        private async Task OnCallbackExceptionAsync(object sender, CallbackExceptionEventArgs e)
        {
            if (_disposed) return;

            _logger.LogWarning($"CallbackException: {e.Exception.Message}");

            // only attempt to reconnect if it's a network error
            if (e.Exception is SocketException)
            {
                await SafeReconnectAsync("CallbackException");
            }
        }

        /// <summary>
        /// Handles the ConnectionBlocked event from RabbitMQ. This can occur when the server is under resource pressure. In this case, we log the reason but do not attempt to reconnect immediately, as the server may recover on its own.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// <returns> A task that represents the asynchronous operation.</returns>
        private Task OnConnectionBlockedAsync(object sender, ConnectionBlockedEventArgs e)
        {
            _logger.LogWarning($"Connection Blocked: {e.Reason}");

            // do not attempt to reconnect here
            return Task.CompletedTask;
        }

        /// <summary>
        /// Safely attempts to reconnect to RabbitMQ, ensuring that we do not attempt to reconnect if the object has been disposed. This method is called from the event handlers when a connection issue is detected.
        /// </summary>
        /// <param name="reason"></param>
        /// <returns> A task that represents the asynchronous operation.</returns>
        private async Task SafeReconnectAsync(string reason)
        {
            if (_disposed) return;

            _logger.LogWarning($"Attempting to reconnect ({reason})...");

            await TryConnectAsync();
        }

        /// <summary>
        /// Disposes the RabbitConnection, closing the connection to RabbitMQ and unsubscribing from events. This method is idempotent and can be called multiple times without throwing exceptions.
        /// </summary>
        public void Dispose()
        {
            if (_disposed) return;

            _disposed = true;

            try
            {
                _connection?.Dispose();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error closing connection");
            }
        }
    }
}
