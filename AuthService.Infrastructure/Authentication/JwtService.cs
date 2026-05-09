using AuthService.Application.DTOs;
using AuthService.Application.Interfaces.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace AuthService.Infrastructure.Authentication
{
    /// <summary>
    /// Service responsible for generating JWT tokens for authenticated users, implementing the IJwtService interface.
    /// </summary>
    public class JwtService : IJwtService
    {
        private readonly JwtOptions _jwtOptions;

        /// <summary>
        /// Initializes a new instance of the <see cref="JwtService"/> class.
        /// </summary>
        /// <param name="configuration">Application configuration from which JWT options are loaded.</param>
        public JwtService(IConfiguration configuration)
        {
            _jwtOptions = configuration.GetSection("Jwt").Get<JwtOptions>()!;
        }

        /// <summary>
        /// Generates a JWT token for the specified user, embedding user information and standard claims.
        /// </summary>
        /// <param name="user">The user information for whom the token is being generated, including Id and Email.</param>
        /// <param name="expires">The expiration time for the generated token.</param>
        /// <returns> A JWT token string that can be used for authentication and authorization.</returns>
        public string GenerateJwtToken(AuthUserGenerateTokenDto user, DateTimeOffset expires)
        {
            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, _jwtOptions!.Subject),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64),
                new Claim("Id", user.Id.ToString()),
                new Claim("Email", user.Email),
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtOptions.Key));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                _jwtOptions.Issuer,
                _jwtOptions.Audience,
                claims,
                expires: expires.UtcDateTime,
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
