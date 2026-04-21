using SharedKernel.Pagination;
using UserService.Application.DTOs;

namespace UserService.Application.Interfaces.Services
{
    /// <summary>
    /// Defines the contract for a service that provides asynchronous operations for retrieving, creating, and
    /// updating user profiles.
    /// </summary>
    /// <remarks>Implementations of this interface are responsible for managing user profile data, including
    /// querying by identifier, filtering with parameters, and supporting paging. All operations are asynchronous and
    /// return tasks that complete when the underlying data store operations finish. Methods may return null or empty
    /// results if no matching users are found. Implementations should enforce parameter validation as described in
    /// method documentation.</remarks>
    public interface IUserService
    {
        /// <summary>
        /// Asynchronously retrieves the user profile associated with the specified unique identifier.
        /// </summary>
        /// <param name="Id">The unique identifier of the user whose profile is to be retrieved.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains a <see cref="UserProfileDto"/>
        /// representing the user's profile, or <see langword="null"/> if no user with the specified identifier exists.</returns>
        Task<UserProfileDto> GetUserProfileByIdAsync(Guid Id);

        /// <summary>
        /// Asynchronously retrieves a paginated list of user profiles based on the provided query parameters.
        /// </summary>
        /// <param name="queryParameters"></param>
        /// <returns> A task that represents the asynchronous operation. The task result contains a <see cref="PagedResult{UserProfileDto}"/>
        /// representing a paginated list of user profiles that match the specified query parameters, including pagination metadata such as total items, current page, and total pages.</returns>
        Task<PagedResult<UserProfileDto>> GetUsersAsync(UserProfileQueryParametersDto queryParameters);

        /// <summary>
        /// Asynchronously creates a new user profile with the specified unique identifier and data transfer object.
        /// </summary>
        /// <param name="dto"></param>
        /// <returns> A task that represents the asynchronous operation. The task completes when the user profile has been created in the underlying data store. Implementations should ensure that the provided unique identifier is not already associated with an existing user profile and that the data transfer object contains valid information for creating a new user profile.</returns>
        Task CreateAsync(CreateUserProfileDto dto);

        /// <summary>
        /// Asynchronously updates the user profile for the specified user.
        /// </summary>
        /// <param name="userId">The unique identifier of the user whose profile is to be updated.</param>
        /// <param name="dto">An object containing the updated profile information to apply to the user.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains a UserProfileDto with the
        /// updated user profile information.</returns>
        Task<UserProfileDto> UpdateAsync(Guid userId, UpdateUserProfileDto dto);
    }
}
