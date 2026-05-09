using AuthService.Application.Interfaces.Repositories;
using AuthService.Application.Interfaces.Services;
using AuthService.Infrastructure.Authentication;
using AuthService.Infrastructure.Data;
using AuthService.Infrastructure.Messaging;
using AuthService.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace AuthService.Infrastructure
{
    /// <summary>
    /// Extension methods for setting up infrastructure services in the DI container.
    /// </summary>
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            // Register DbContext
            services.AddDbContext<AuthDbContext>(options =>
                options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));

            // Configure RabbitMQ settings and services
            services.Configure<RabbitMQSettings>(configuration.GetSection("RabbitMQ"));

            // Connection factory for RabbitMQ
            services.AddSingleton<IConnectionFactory>(sp =>
            {
                var settings = sp.GetRequiredService<IOptions<RabbitMQSettings>>().Value;
                return new ConnectionFactory
                {
                    HostName = settings.HostName,
                    UserName = settings.UserName,
                    Password = settings.Password,
                    Port = settings.Port
                };
            });

            // Register RabbitMQ connection service
            services.AddSingleton<IRabbitConnection, RabbitConnection>();

            // Register repositories, if any
            services.AddScoped<IAuthUserRepository, AuthUserRepository>();

            // Authentication services
            services.AddSingleton<IJwtService, JwtService>();

            return services;
        }
    }
}
