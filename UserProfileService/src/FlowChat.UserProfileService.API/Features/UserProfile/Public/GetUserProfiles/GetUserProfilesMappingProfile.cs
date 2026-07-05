using AutoMapper;
using FlowChat.UserProfileService.Application.Features.UserProfile.Queries.UserProfile.Model;

namespace FlowChat.UserProfileService.Api.Features.UserProfile.Public.GetUserProfiles;

public sealed class GetUserProfilesMappingProfile : Profile
{
    public GetUserProfilesMappingProfile()
    {
        CreateMap<EmailDto, EmailResponse>();
        CreateMap<PhoneDto, PhoneResponse>();
        CreateMap<UserProfileDto, UserProfileResponse>();
    }
}
