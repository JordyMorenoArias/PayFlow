using AuthService.Application.DTOs;
using AuthService.Application.Interfaces.Repositories;
using AuthService.Application.Interfaces.Services;
using AuthService.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace AuthService.Application.Services
{
    /// <summary>
    /// Default implementation of the IAuthUserService interface, providing user authentication and registration functionalities.
    /// </summary>
    public class DefaultAuthUserService : IAuthUserService
    {
        private readonly IAuthUserRepository _authUserRepository;
        private readonly IJwtService _jwtService;
        private readonly IPasswordHasher<AuthUser> _passwordHasher;
        private readonly ILogger<DefaultAuthUserService> _logger;

        public DefaultAuthUserService(IAuthUserRepository authUserRepository, IJwtService jwtService, IPasswordHasher<AuthUser> passwordHasher, ILogger<DefaultAuthUserService> logger)
        {
            _authUserRepository = authUserRepository;
            _jwtService = jwtService;
            _passwordHasher = passwordHasher;
            _logger = logger;
        }

        /// <summary>
        /// Authenticates a user based on the provided email and password, and generates a JWT token if the credentials are valid.
        /// </summary>
        /// <param name="dto"></param>
        /// <param name="cd"></param>
        /// <returns> An AuthUserResultDto containing the generated JWT token and its expiration time.</returns>
        /// <exception cref="UnauthorizedAccessException"></exception>
        async Task<AuthUserResultDto> IAuthUserService.LoginAsync(LoginAuthUserDto dto, CancellationToken cd)
        {
            var authUser = await  _authUserRepository.GetByEmailAsync(dto.Email, cd);

            if (authUser == null)
            {
                _logger.LogWarning("Login attempt failed for email {Email}: user not found", dto.Email);
                throw new UnauthorizedAccessException("Invalid email or password.");
            }
            
            var verificationResult = _passwordHasher.VerifyHashedPassword(authUser, authUser.PasswordHash, dto.Password);

            if (verificationResult == PasswordVerificationResult.Failed)
            {
                _logger.LogWarning("Login attempt failed for email {Email}: invalid password", dto.Email);
                throw new UnauthorizedAccessException("Invalid email or password.");
            }

            var expires = DateTimeOffset.UtcNow.AddHours(1);
            var authUserTokenDto = new AuthUserGenerateTokenDto
            {
                Id = authUser.Id,
                Email = authUser.Email,
            };
            var token = _jwtService.GenerateJwtToken(authUserTokenDto, expires);

            _logger.LogInformation("User {Email} logged in successfully", dto.Email);

            return new AuthUserResultDto
            {
                Token = token,
                Expires = expires
            };

        }

        /// <summary>
        /// Registers a new user with the provided email and password. It checks if a user with the same email already exists, and if not, it creates a new user and stores it in the repository.
        /// </summary>
        /// <param name="dto"></param>
        /// <param name="cd"></param>
        /// <returns> A Task representing the asynchronous operation of user registration.</returns>
        /// <exception cref="InvalidOperationException"></exception>
        async Task IAuthUserService.RegisterAsync(RegisterAuthUserDto dto, CancellationToken cd)
        {
            var authUser = await _authUserRepository.GetByEmailAsync(dto.Email, cd);

            if (authUser != null)
            {
                _logger.LogWarning("Registration attempt failed for email {Email}: user already exists", dto.Email);
                throw new InvalidOperationException("A user with this email already exists.");
            }

            var newAuthUser = new AuthUser(email: dto.Email, passwordHash: string.Empty);
            var passwordHash = _passwordHasher.HashPassword(newAuthUser, dto.Password);
            newAuthUser.UpdatePassword(passwordHash);
            await _authUserRepository.RegisterUserAsync(newAuthUser, cd);
        }
    }
}
