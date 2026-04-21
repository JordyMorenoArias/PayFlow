using AutoMapper;
using SharedKernel.Pagination;
using UserService.Application.DTOs;
using UserService.Application.Exceptions;
using UserService.Application.Interfaces.Repositories;
using UserService.Application.Interfaces.Services;
using UserService.Domain.Entities;

namespace UserService.Application.Services
{
    /// <summary>
    /// Implementation of the IUserService interface that provides methods for managing user profiles. This service interacts with the IUserRepository to perform CRUD operations on user profiles and uses AutoMapper to map between domain entities and DTOs.
    /// </summary>
    public class DefaultUserService : IUserService
    {
        private readonly IUserRepository _userRepository;
        private readonly IMapper _mapper;

        public DefaultUserService(IUserRepository userRepository, IMapper mapper)
        {
            this._userRepository = userRepository;
            this._mapper = mapper;
        }

        /// <summary>
        /// Creates a new user profile based on the provided CreateUserProfileDto. If a user profile with the same ID already exists, it catches the DuplicateEntityException and ignores it to ensure idempotency.
        /// </summary>
        /// <param name="dto"></param>
        /// <returns> A task that represents the asynchronous operation.</returns>
        public async Task CreateAsync(CreateUserProfileDto dto)
        {
            var userProfile = new UserProfile(
                dto.Id,
                dto.FirstName,
                dto.LastName
            );

            try
            {
                await _userRepository.CreateAsync(userProfile);
            }
            catch (DuplicateEntityException)
            {
                // idempotencia -> ya existe -> ignorar
                return;
            }
        }

        /// <summary>
        /// Retrieves a user profile by its unique identifier. If the user profile is not found, it throws a KeyNotFoundException.
        /// </summary>
        /// <param name="Id"></param>
        /// <returns> A task that represents the asynchronous operation, containing the UserProfileDto if found.</returns>
        /// <exception cref="KeyNotFoundException"></exception>
        public async Task<UserProfileDto> GetUserProfileByIdAsync(Guid Id)
        {
            var userProfile = await _userRepository.GetUserProfileByIdAsync(Id);
            
            if (userProfile == null)
            {
                throw new KeyNotFoundException($"User profile with id {Id} not found.");
            }

            return _mapper.Map<UserProfileDto>(userProfile);
        }

        /// <summary>
        /// Retrieves a paginated list of user profiles based on the provided query parameters. It uses the IUserRepository to fetch the data and AutoMapper to map the domain entities to DTOs before returning the paginated result.
        /// </summary>
        /// <param name="queryParameters"></param>
        /// <returns> A task that represents the asynchronous operation, containing a PagedResult of UserProfileDto.</returns>
        public async Task<PagedResult<UserProfileDto>> GetUsersAsync(UserProfileQueryParametersDto queryParameters)
        {
            var pagedResult = await _userRepository.GetUsersAsync(queryParameters);

            return new PagedResult<UserProfileDto>
            {
                Items = _mapper.Map<IEnumerable<UserProfileDto>>(pagedResult.Items),
                TotalItems = pagedResult.TotalItems,
                Page = pagedResult.Page,
                PageSize = pagedResult.PageSize
            };
        }

        /// <summary>
        /// Updates an existing user profile based on the provided UpdateUserProfileDto. It first retrieves the user profile by its ID, and if it exists, it updates the profile with the new information. If the user profile is not found, it throws a KeyNotFoundException.
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="request"></param>
        /// <returns> A task that represents the asynchronous operation, containing the updated UserProfileDto.</returns>
        /// <exception cref="KeyNotFoundException"></exception>
        public async Task<UserProfileDto> UpdateAsync(Guid userId, UpdateUserProfileDto request)
        {
            var userProfile = await _userRepository.GetUserProfileByIdAsync(userId);

            if (userProfile == null)
            {
                throw new KeyNotFoundException($"User profile with id {userId} not found.");
            }

            userProfile.Update(request.FirstName, request.LastName);

            var updatedUserProfile = await _userRepository.UpdateAsync(userProfile);

            return _mapper.Map<UserProfileDto>(updatedUserProfile);
        }
    }
}
