using AuthService.Application.DTOs;

namespace AuthService.Application.Interfaces.Services
{
    /// <summary>
    /// Service interface for handling authentication-related operations for users, such as login and registration.
    /// </summary>
    public interface IAuthUserService
    {
        /// <summary>
        /// Authenticates a user based on the provided login credentials. Returns true if authentication is successful, otherwise false.
        /// </summary>
        /// <param name="dto"></param>
        /// <param name="cd"></param>
        /// <returns> An AuthUserResultDto containing the authentication result, including success status and any relevant data such as tokens or error messages.</returns>
        Task<AuthUserResultDto> LoginAsync(LoginAuthUserDto dto, CancellationToken cd = default);

        /// <summary>
        /// Registers a new user with the provided registration details. Returns true if registration is successful, otherwise false.
        /// </summary>
        /// <param name="dto"></param>
        /// <param name="cd"></param>
        /// <returns> A boolean value indicating whether the registration was successful or not. If registration fails, it may throw exceptions with relevant error messages.</returns>
        Task RegisterAsync(RegisterAuthUserDto dto, CancellationToken cd = default);
    }
}
