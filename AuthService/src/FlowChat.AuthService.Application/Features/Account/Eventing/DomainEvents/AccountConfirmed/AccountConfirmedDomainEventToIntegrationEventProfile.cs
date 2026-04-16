using AutoMapper;
using FlowChat.AuthService.Domain.Entities.Account.Events;
using FlowChat.Core.Messaging.AuthService.Events;

namespace FlowChat.AuthService.Application.Features.Account.Eventing.DomainEvents.AccountConfirmed;

public sealed class AccountConfirmedDomainEventToIntegrationEventProfile : Profile
{
    public AccountConfirmedDomainEventToIntegrationEventProfile()
    {
        CreateMap<AccountConfirmedDomainEvent, AccountConfirmedIntegrationEvent>()
            .ForMember(destination => destination.Key, options => options.MapFrom(source => source.AccountId.Value.ToString()))
            .ForMember(destination => destination.UserId, options => options.MapFrom(source => source.AccountId.Value));
    }
}
