using AutoMapper;
using UserService.Application.DTOs;
using UserService.Domain.Entities;

namespace UserService.Application.Mappings
{
    public class UserProfileMapping : Profile
    {
        public UserProfileMapping()
        {
            CreateMap<UserProfile, UserProfileDto>().ReverseMap();
            CreateMap<CreateUserProfileDto, UserProfile>();
            CreateMap<UpdateUserProfileDto, UserProfile>();
        }
    }
}
