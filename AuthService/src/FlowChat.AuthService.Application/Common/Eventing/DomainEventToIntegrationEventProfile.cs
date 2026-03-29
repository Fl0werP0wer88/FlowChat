using AutoMapper;
using FlowChat.AuthService.Domain.Events;
using FlowChat.Core.Messaging.AuthService.Events;

namespace FlowChat.AuthService.Application.Common.Eventing;

public sealed class DomainEventToIntegrationEventProfile : Profile
{
    public DomainEventToIntegrationEventProfile()
    {
        CreateMap<AccountRegisteredDomainEvent, AccountRegisteredIntegrationEvent>()
            .ForMember(destination => destination.Key, options => options.MapFrom(source => source.UserId.Value.ToString()))
            .ForMember(destination => destination.UserId, options => options.MapFrom(source => source.UserId.Value))
            .ForMember(destination => destination.DisplayName, options => options.MapFrom(source => source.UserName));

        CreateMap<AccountConfirmedDomainEvent, UserConfirmedIntegrationEvent>()
            .ForMember(destination => destination.Key, options => options.MapFrom(source => source.UserId.Value.ToString()))
            .ForMember(destination => destination.UserId, options => options.MapFrom(source => source.UserId.Value));

        CreateMap<PhoneNumberConfirmedDomainEvent, PhoneNumberConfirmedIntegrationEvent>()
            .ForMember(destination => destination.Key, options => options.MapFrom(source => source.UserId.Value.ToString()))
            .ForMember(destination => destination.UserId, options => options.MapFrom(source => source.UserId.Value));
    }
}
