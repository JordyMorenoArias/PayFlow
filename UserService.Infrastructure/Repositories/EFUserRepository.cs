using Microsoft.EntityFrameworkCore;
using SharedKernel.Pagination;
using UserService.Application.DTOs;
using UserService.Application.Interfaces.Repositories;
using UserService.Domain.Entities;
using UserService.Infrastructure.Data;

namespace UserService.Infrastructure.Repositories
{
    /// <summary>
    /// Provides an Entity Framework-based implementation of the IUserRepository interface for managing user profiles in
    /// a persistent data store.
    /// </summary>
    /// <remarks>This repository enables asynchronous operations for creating, retrieving, updating, and
    /// querying user profiles using Entity Framework Core. It supports paginated and filtered retrieval of users.
    /// Instances of this class should be used with a properly configured UserDbContext. This class is intended for use
    /// in applications that require persistent storage and retrieval of user profile data.</remarks>
    public class EFUserRepository : IUserRepository
    {
        private readonly UserDbContext _context;

        EFUserRepository(UserDbContext context)
        {
            this._context = context;
        }

        /// <summary>
        /// Asynchronously creates a new user profile and saves it to the data store.
        /// </summary>
        /// <param name="userProfile">The user profile to create. Cannot be null.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the created user profile.</returns>
        public async Task<UserProfile> CreateAsync(UserProfile userProfile)
        {
            _context.Users.Add(userProfile);
            await _context.SaveChangesAsync();
            return userProfile;
        }

        /// <summary>
        /// Asynchronously retrieves the user profile associated with the specified user identifier.
        /// </summary>
        /// <param name="userId">The unique identifier of the user whose profile is to be retrieved.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the user profile if found;
        /// otherwise, null.</returns>
        public async Task<UserProfile?> GetUserProfileByIdAsync(Guid userId)
        {
            return await _context.Users.FindAsync(userId);
        }

        /// <summary>
        /// Asynchronously retrieves a paged list of user profiles that match the specified query parameters.
        /// </summary>
        /// <remarks>The search is case-insensitive and matches the search term against both first and
        /// last names. Paging parameters determine which subset of results is returned. The method does not track
        /// changes to the returned user profiles.</remarks>
        /// <param name="queryParameters">The parameters used to filter, search, and paginate the user profiles. Must not be null. The search term is
        /// applied to first and last names; paging is controlled by the page and page size values.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains a paged result of user profiles
        /// matching the query parameters. The result includes the total number of matching items and the current page
        /// of user profiles.</returns>
        public async Task<PagedResult<UserProfile>> GetUsersAsync(UserProfileQueryParametersDto queryParameters)
        {
            var query = _context.Users.AsNoTracking().AsQueryable();

            if (!string.IsNullOrEmpty(queryParameters.SearchTerm))
            {
                var filter = queryParameters.SearchTerm.Trim();
                query = query.Where(u =>
                    u.FirstName.Contains(filter) ||
                    u.LastName.Contains(filter));
            }

            var totalItems = await query.CountAsync();
            var items = await query
                .Skip((queryParameters.Page - 1) * queryParameters.PageSize)
                .Take(queryParameters.PageSize)
                .ToListAsync();

            return new PagedResult<UserProfile>
            {
                Items = items,
                TotalItems = totalItems,
                Page = queryParameters.Page,
                PageSize = queryParameters.PageSize,
            };
        }

        /// <summary>
        /// Asynchronously updates the specified user profile in the data store.
        /// </summary>
        /// <param name="userProfile">The user profile entity to update. Cannot be null. The entity must have a valid identifier corresponding to
        /// an existing user.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the updated user profile entity.</returns>
        public async Task<UserProfile> UpdateAsync(UserProfile userProfile)
        {
            _context.Users.Update(userProfile);
            await _context.SaveChangesAsync();
            return userProfile;
        }
    }
}
