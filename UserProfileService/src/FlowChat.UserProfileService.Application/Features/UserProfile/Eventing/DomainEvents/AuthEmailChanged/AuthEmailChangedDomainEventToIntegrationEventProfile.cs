using AutoMapper;
using FlowChat.Core.Messaging.UserProfileService.Events;
using FlowChat.UserProfileService.Domain.Entities.UserProfile.Events;

namespace FlowChat.UserProfileService.Application.Features.UserProfile.Eventing.DomainEvents.AuthEmailChanged;

public sealed class AuthEmailChangedDomainEventToIntegrationEventProfile : Profile
{
    public AuthEmailChangedDomainEventToIntegrationEventProfile()
    {
        CreateMap<AuthEmailChangedDomainEvent, AuthEmailChangedIntegrationEvent>()
            .ForMember(destination => destination.Key, options => options.MapFrom(source => source.UserProfileId.Value.ToString()))
            .ForMember(destination => destination.UserProfileId, options => options.MapFrom(source => source.UserProfileId.Value))
            .ForMember(destination => destination.EmailId, options => options.MapFrom(source => source.EmailId.Value))
            .ForMember(destination => destination.EmailAddress, options => options.MapFrom(source => source.Address.Value));
    }
}
