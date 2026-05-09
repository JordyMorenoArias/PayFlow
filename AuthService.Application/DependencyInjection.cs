using AuthService.Application.Interfaces.Services;
using AuthService.Application.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace AuthService.Application
{
    /// <summary>
    /// Provides extension methods for registering application services and configuring JWT authentication in the dependency injection container.
    /// </summary>
    public static class DependencyInjection
    {
        public static IServiceCollection AddApplication(this IServiceCollection services, IConfiguration configuration)
        {
            // JWT Authentication
            var jwtKey = configuration["JWT:KEY"];

            if (string.IsNullOrEmpty(jwtKey))
            {
                throw new Exception("JWT:KEY is not configured");
            }

            var jwtIssuer = configuration["JWT:ISSUER"];

            if (string.IsNullOrEmpty(jwtIssuer))
            {
                throw new Exception("JWT:ISSUER is not configured");
            }

            var jwtAudience = configuration["JWT:AUDIENCE"];

            if (string.IsNullOrEmpty(jwtAudience))
            {
                throw new Exception("JWT:AUDIENCE is not configured");
            }

            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,

                    ValidIssuer = jwtIssuer,
                    ValidAudience = jwtAudience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
                };
            });

            // Aquí puedes registrar otros servicios de aplicación
            services.AddScoped<IAuthUserService, DefaultAuthUserService>();

            return services;
        }
    }
}
