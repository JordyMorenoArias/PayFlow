using Microsoft.Extensions.DependencyInjection;
using UserService.Application.Mappings;

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
            // services.AddScoped<IUserService, UserService>();

            return services;
        }
    }
}
