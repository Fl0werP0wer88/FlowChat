using AutoMapper;
using FlowChat.UserProfileService.Application.UserProfiles.Queries;
using FlowChat.UserProfileService.Domain.Entities;

namespace FlowChat.UserProfileService.Infrastructure.Mapping;

public sealed class UserProfileMappingProfile : Profile
{
    public UserProfileMappingProfile()
    {
        CreateMap<UserProfile, UserProfileDto>();
    }
}
