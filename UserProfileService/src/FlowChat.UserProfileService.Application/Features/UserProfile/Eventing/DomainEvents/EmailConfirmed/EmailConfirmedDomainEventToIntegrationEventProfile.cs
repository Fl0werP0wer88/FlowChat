using AutoMapper;
using FlowChat.Core.Messaging.UserProfileService.Events;
using FlowChat.UserProfileService.Domain.Entities.UserProfile.Events;

namespace FlowChat.UserProfileService.Application.Features.UserProfile.Eventing.DomainEvents.EmailConfirmed;

public sealed class EmailConfirmedDomainEventToIntegrationEventProfile : Profile
{
    public EmailConfirmedDomainEventToIntegrationEventProfile()
    {
        CreateMap<EmailConfirmedDomainEvent, UserEmailConfirmedIntegrationEvent>()
            .ForMember(destination => destination.UserProfileId, options => options.MapFrom(source => source.UserProfileId.Value))
            .ForMember(destination => destination.EmailId, options => options.MapFrom(source => source.EmailId.Value))
            .ForMember(destination => destination.Email, options => options.MapFrom(source => new FlowChat.Core.Messaging.UserProfileService.Events.Email
            {
                Address = source.Email.Value,
                IsAuth = source.IsAuth
            }));
    }
}
