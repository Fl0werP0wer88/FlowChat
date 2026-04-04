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
            .ForMember(destination => destination.DisplayName, options => options.MapFrom(source => source.FriendlyUserId))
            .ForMember(destination => destination.PhoneNumber, options => options.MapFrom(_ => (string?)null))
            .ForMember(destination => destination.FirstName, options => options.MapFrom(_ => (string?)null))
            .ForMember(destination => destination.LastName, options => options.MapFrom(_ => (string?)null));

        CreateMap<AccountConfirmedDomainEvent, AccountConfirmedIntegrationEvent>()
            .ForMember(destination => destination.Key, options => options.MapFrom(source => source.AccountId.Value.ToString()))
            .ForMember(destination => destination.UserId, options => options.MapFrom(source => source.AccountId.Value));
    }
}
