namespace AuthService.Infrastructure.Messaging
{
    public class RabbitMQSettings
    {
        public string HostName { get; set; } = string.Empty;
        public int Port { get; set; } = 5672; // Default RabbitMQ port
        public string UserName { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }
}
