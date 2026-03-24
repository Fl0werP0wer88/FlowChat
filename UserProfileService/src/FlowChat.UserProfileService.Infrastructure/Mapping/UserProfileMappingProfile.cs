using AutoMapper;
using FlowChat.UserProfileService.Application.UserProfiles.Queries.GetUserProfile;
using FlowChat.UserProfileService.Domain.Entities;

namespace FlowChat.UserProfileService.Infrastructure.Mapping;

public sealed class UserProfileMappingProfile : Profile
{
    public UserProfileMappingProfile()
    {
        CreateMap<Email, EmailDto>()
            .ForCtorParam(nameof(EmailDto.Id), opt => opt.MapFrom(src => src.Id.Value))
            .ForCtorParam(nameof(EmailDto.Address), opt => opt.MapFrom(src => src.Address.Value));

        CreateMap<Phone, PhoneDto>()
            .ForCtorParam(nameof(PhoneDto.Id), opt => opt.MapFrom(src => src.Id.Value))
            .ForCtorParam(nameof(PhoneDto.Number), opt => opt.MapFrom(src => src.Number.Value));

        CreateMap<UserProfile, UserProfileDto>()
            .ForCtorParam(nameof(UserProfileDto.Id), opt => opt.MapFrom(src => src.Id.Value))
            .ForCtorParam(nameof(UserProfileDto.Emails), opt => opt.MapFrom(src => src.Emails))
            .ForCtorParam(nameof(UserProfileDto.Phones), opt => opt.MapFrom(src => src.Phones));
    }
}
