using AutoMapper;
using FlowChat.Core.Messaging.UserProfileService.Events;
using FlowChat.UserProfileService.Domain.Entities.UserProfile.Events;

namespace FlowChat.UserProfileService.Application.Features.UserProfile.Eventing.DomainEvents.UserProfileDeleted;

public sealed class UserProfileDeletedDomainEventToIntegrationEventProfile : Profile
{
    public UserProfileDeletedDomainEventToIntegrationEventProfile()
    {
        CreateMap<UserProfileDeletedDomainEvent, UserProfileDeletedIntegrationEvent>()
            .ForMember(dst => dst.UserProfileId, opt => opt.MapFrom(src => src.UserProfileId.Value));
    }
}
