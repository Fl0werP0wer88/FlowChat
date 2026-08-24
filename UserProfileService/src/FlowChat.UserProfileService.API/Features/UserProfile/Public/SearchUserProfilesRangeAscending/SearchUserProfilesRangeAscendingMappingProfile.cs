using AutoMapper;
using FlowChat.UserProfileService.Application.Features.UserProfile.Queries.UserProfile.Model;
using FlowChat.UserProfileService.Application.Features.UserProfile.Queries.UserProfile.SearchUserProfilesRangeAscending;

namespace FlowChat.UserProfileService.Api.Features.UserProfile.Public.SearchUserProfilesRangeAscending;

public sealed class SearchUserProfilesRangeAscendingMappingProfile : Profile
{
    public SearchUserProfilesRangeAscendingMappingProfile()
    {
        CreateMap<UserProfileSearchResultDto, UserProfileSearchResultResponse>();
        CreateMap<SearchUserProfilesRangeAscendingPageDto, SearchUserProfilesRangeAscendingResponse>();
    }
}
