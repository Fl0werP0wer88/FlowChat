using AutoMapper;
using FlowChat.AuthService.Domain.Entities.Account.Events;
using FlowChat.Core.Messaging.AuthService.Events;

namespace FlowChat.AuthService.Application.Common.Eventing;

public sealed class DomainEventToIntegrationEventProfile : Profile
{
    public DomainEventToIntegrationEventProfile()
    {
        CreateMap<AccountRegisteredDomainEvent, AccountRegisteredIntegrationEvent>()
            .ForMember(destination => destination.Key, options => options.MapFrom(source => source.AccountId.Value.ToString()))
            .ForMember(destination => destination.UserId, options => options.MapFrom(source => source.AccountId.Value))
            .ForMember(destination => destination.Email, options => options.MapFrom(source => source.Email.Value))
            .ForMember(destination => destination.FriendlyUserId, options => options.MapFrom(source => source.FriendlyUserId))
            .ForMember(destination => destination.FirstName, options => options.MapFrom(source => source.FirstName))
            .ForMember(destination => destination.LastName, options => options.MapFrom(source => source.LastName))
            .ForMember(destination => destination.Organization, options => options.MapFrom(source => source.Organization));

        CreateMap<AccountConfirmedDomainEvent, AccountConfirmedIntegrationEvent>()
            .ForMember(destination => destination.Key, options => options.MapFrom(source => source.AccountId.Value.ToString()))
            .ForMember(destination => destination.UserId, options => options.MapFrom(source => source.AccountId.Value));
    }
}
