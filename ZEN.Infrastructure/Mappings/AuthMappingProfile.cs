using AutoMapper;
using ZEN.Application.DTOs.Auth;
using ZEN.Infrastructure.Identity;

namespace ZEN.Infrastructure.Mappings;

public class AuthMappingProfile : Profile
{
    public AuthMappingProfile()
    {
        CreateMap<AppUser, UserDto>()
            .ForMember(d => d.Roles, opt => opt.Ignore()); // Roles need to be populated separately via UserManager

        CreateMap<RegisterUserRequest, AppUser>()
            .ForMember(d => d.UserName, opt => opt.MapFrom(s => s.Email))
            .ForMember(d => d.EmailConfirmed, opt => opt.MapFrom(s => true))
            .ForMember(d => d.Id, opt => opt.Ignore())
            .ForMember(d => d.NormalizedUserName, opt => opt.Ignore())
            .ForMember(d => d.NormalizedEmail, opt => opt.Ignore())
            .ForMember(d => d.PasswordHash, opt => opt.Ignore())
            .ForMember(d => d.SecurityStamp, opt => opt.Ignore())
            .ForMember(d => d.ConcurrencyStamp, opt => opt.Ignore());
    }
}
