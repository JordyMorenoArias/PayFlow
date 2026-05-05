using RabbitMQ.Client;

namespace AuthService.Infrastructure.Messaging
{
    /// <summary>
    /// Interface for managing RabbitMQ connections and channels.
    /// </summary>
    public interface IRabbitConnection : IDisposable
    {
        bool IsConnected { get; }

        Task<bool> TryConnectAsync(CancellationToken cancellationToken = default);

        Task<IChannel> CreateChannelAsync();
    }
}
