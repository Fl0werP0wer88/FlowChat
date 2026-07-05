using AutoMapper;
using FlowChat.UserProfileService.Application.Features.UserProfile.Queries.UserProfile.Model;

namespace FlowChat.UserProfileService.Api.Features.UserProfile.Public.SearchUserProfiles;

public sealed class SearchUserProfilesMappingProfile : Profile
{
    public SearchUserProfilesMappingProfile()
    {
        CreateMap<EmailDto, EmailResponse>();
        CreateMap<PhoneDto, PhoneResponse>();
        CreateMap<UserProfileDto, UserProfileResponse>();
    }
}
