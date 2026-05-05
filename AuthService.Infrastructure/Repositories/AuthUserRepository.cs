using AuthService.Application.Interfaces.Repositories;
using AuthService.Domain.Entities;
using AuthService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AuthService.Infrastructure.Repositories
{
    /// <summary>
    /// Repository for managing AuthUser entities in the database.
    /// </summary>
    public class AuthUserRepository : IAuthUserRepository
    {
        private readonly AuthDbContext _context;

        public AuthUserRepository(AuthDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Retrieves an AuthUser by their email address.
        /// </summary>
        /// <param name="email"></param>
        /// <param name="cd"></param>
        /// <returns> The AuthUser with the specified email, or null if not found.</returns>
        public async Task<AuthUser?> GetByEmailAsync(string email, CancellationToken cd = default)
        {
            return await _context.Users
                .Where(u => u.Email == email)
                .FirstOrDefaultAsync(cd);
        }

        /// <summary>
        /// Checks if an AuthUser with the specified email already exists in the database.
        /// </summary>
        /// <param name="email"></param>
        /// <param name="cd"></param>
        /// <returns> True if an AuthUser with the email exists, otherwise false.</returns>
        public async Task<bool> IsEmailExistsAsync(string email, CancellationToken cd = default)
        {
            return await _context.Users
                .AnyAsync(u => u.Email == email, cd);
        }

        /// <summary>
        /// Registers a new AuthUser in the database. If the email already exists, it will throw an exception.
        /// </summary>
        /// <param name="user"></param>
        /// <param name="cd"></param>
        /// <returns> The registered AuthUser entity.</returns>
        public async Task<AuthUser?> RegisterUserAsync(AuthUser user, CancellationToken cd = default)
        {
            _context.Users.Add(user);
            await _context.SaveChangesAsync(cd);
            return user;
        }

        /// <summary>
        /// Updates an existing AuthUser in the database. If the user does not exist, it will throw an exception.
        /// </summary>
        /// <param name="user"></param>
        /// <param name="cd"></param>
        /// <returns> The updated AuthUser entity.</returns>
        public async Task<AuthUser?> UpdateUserAsync(AuthUser user, CancellationToken cd = default)
        {
            _context.Users.Update(user);
            await _context.SaveChangesAsync(cd);
            return user;
        }
    }
}
