using AuthService.Application.DTOs;

namespace AuthService.Application.Interfaces.Services
{
    /// <summary>
    /// Interface for JWT (JSON Web Token) generation service.
    /// </summary>
    public interface IJwtService
    {
        /// <summary>
        /// Generates a JWT token for a user.
        /// </summary>
        /// <param name="user">The user information for whom the token is being generated, including Id and Email.</param>
        /// <param name="expires">The expiration time for the generated token.</param>
        /// <returns>A string representing the generated JWT token.</returns>
        public string GenerateJwtToken(AuthUserGenerateTokenDto user, DateTimeOffset expires);
    }
}
