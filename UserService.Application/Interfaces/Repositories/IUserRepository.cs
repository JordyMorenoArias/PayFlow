using SharedKernel.Pagination;
using UserService.Application.DTOs;
using UserService.Domain.Entities;

namespace UserService.Application.Interfaces.Repositories
{
    /// <summary>
    /// Defines the contract for a repository that provides asynchronous operations for retrieving, creating, and
    /// updating user profiles.
    /// </summary>
    /// <remarks>Implementations of this interface are responsible for managing user profile data, including
    /// querying by identifier, filtering with parameters, and supporting paging. All operations are asynchronous and
    /// return tasks that complete when the underlying data store operations finish. Methods may return null or empty
    /// results if no matching users are found. Implementations should enforce parameter validation as described in
    /// method documentation.</remarks>
    public interface IUserRepository
    {
        /// <summary>
        /// Asynchronously retrieves the user profile associated with the specified unique user identifier.
        /// </summary>
        /// <param name="userId">The unique identifier of the user whose profile is to be retrieved.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the <see cref="UserProfile"/>
        /// for the specified user identifier, or <c>null</c> if no matching user is found.</returns>
        Task<UserProfile?> GetUserProfileByIdAsync(Guid userId);

        /// <summary>
        /// Asynchronously retrieves a paged list of user profiles that match the specified query parameters.
        /// </summary>
        /// <param name="queryParameters">An object containing filtering, sorting, and paging options to apply when retrieving user profiles. Cannot
        /// be null.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains a <see
        /// cref="PagedResult{UserProfile}"/> with the user profiles matching the query parameters. The result may be
        /// empty if no users match the criteria.</returns>
        Task<PagedResult<UserProfile>> GetUsersAsync(UserProfileQueryParametersDto queryParameters);

        /// <summary>
        /// Asynchronously creates a new user profile in the data store.
        /// </summary>
        /// <param name="userProfile">The user profile to create. Cannot be null.</param>
        /// <returns>A task that represents the asynchronous create operation.</returns>
        Task CreateAsync(UserProfile userProfile);

        /// <summary>
        /// Asynchronously updates the specified user profile in the data store.
        /// </summary>
        /// <param name="userProfile">The user profile to update. Cannot be null. The profile must contain a valid identifier.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the updated user profile.</returns>
        Task<UserProfile> UpdateAsync(UserProfile userProfile);
    }
}
