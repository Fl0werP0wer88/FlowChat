using AutoMapper;
using FlowChat.UserProfileService.Application.Features.UserProfile.Queries.UserProfile.Model;

namespace FlowChat.UserProfileService.Api.Features.UserProfile.Public.GetUserProfile;

public sealed class GetUserProfileMappingProfile : Profile
{
    public GetUserProfileMappingProfile()
    {
        CreateMap<EmailDto, EmailResponse>();
        CreateMap<PhoneDto, PhoneResponse>();
        CreateMap<UserProfileDto, UserProfileResponse>();
    }
}
