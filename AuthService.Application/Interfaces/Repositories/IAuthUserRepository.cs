using AuthService.Domain.Entities;

namespace AuthService.Application.Interfaces.Repositories
{
    /// <summary>
    /// Repository interface for managing authentication users in the AuthService application.
    /// </summary>
    public interface IAuthUserRepository
    {
        /// <summary>
        /// Checks if an email address already exists in the system.
        /// </summary>
        /// <param name="email"></param>
        /// <param name="cd"></param>
        /// <returns> True if the email exists; otherwise, false.</returns>
        Task<bool> IsEmailExistsAsync(string email, CancellationToken cd = default);

        /// <summary>
        /// Retrieves an authentication user by their email address.
        /// </summary>
        /// <param name="email"></param>
        /// <param name="cd"></param>
        /// <returns> The authentication user if found; otherwise, null.</returns>
        Task<AuthUser?> GetByEmailAsync(string email, CancellationToken cd = default);

        /// <summary>
        /// Registers a new authentication user in the system.
        /// </summary>
        /// <param name="user"></param>
        /// <param name="cd"></param>
        /// <returns> The registered authentication user if successful; otherwise, null.</returns>
        Task<AuthUser?> RegisterUserAsync(AuthUser user, CancellationToken cd = default);

        /// <summary>
        /// Updates an existing authentication user's information in the system.
        /// </summary>
        /// <param name="user"></param>
        /// <param name="cd"></param>
        /// <returns> The updated authentication user if successful; otherwise, null.</returns>
        Task<AuthUser?> UpdateUserAsync(AuthUser user, CancellationToken cd = default);
    }
}
