using Microsoft.Extensions.DependencyInjection;
using UserService.Application.Interfaces.Services;
using UserService.Application.Mappings;
using UserService.Application.Services;

namespace UserService.Application
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddApplication(this IServiceCollection services)
        {
            // Registrar AutoMapper escaneando el assembly
            services.AddAutoMapper(cfg =>
            {
                cfg.AddProfile<UserProfileMapping>();
            });

            // Aquí puedes registrar otros servicios de aplicación
            services.AddScoped<IUserService, DefaultUserService>();

            return services;
        }
    }
}
