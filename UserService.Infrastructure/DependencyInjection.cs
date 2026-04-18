using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using UserService.Application.Interfaces.Repositories;
using UserService.Infrastructure.Data;
using UserService.Infrastructure.Persistence;
using UserService.Infrastructure.Repositories;

namespace UserService.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddDbContext<UserDbContext>(options =>
                options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));

            // Register infrastructure services here
            services.AddScoped<IUserRepository, EFUserRepository>();
            services.AddSingleton<IDatabaseExceptionDetector, SqlServerExceptionDetector>();

            return services;
        }
    }
}
