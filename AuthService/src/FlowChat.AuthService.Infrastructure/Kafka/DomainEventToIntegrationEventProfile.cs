using AutoMapper;
using FlowChat.AuthService.Domain.Events;
using FlowChat.Messaging.Contracts;
using FlowChat.Messaging.Contracts.AuthService.Events;

namespace FlowChat.AuthService.Infrastructure.Kafka;

public sealed class DomainEventToIntegrationEventProfile : Profile
{
    public DomainEventToIntegrationEventProfile()
    {
        CreateMap<UserCreatedDomainEvent, UserCreatedIntegrationEvent>()
            .ForMember(destination => destination.UserId, options => options.MapFrom(source => source.UserId.Value))
            .ForMember(destination => destination.DisplayName, options => options.MapFrom(source => source.UserName));

        CreateMap<AccountConfirmedDomainEvent, UserConfirmedIntegrationEvent>()
            .ForMember(destination => destination.UserId, options => options.MapFrom(source => source.UserId.Value));

        CreateMap<EmailVerificationRequestedDomainEvent, EmailVerificationRequestIntegrationEvent>()
            .ForMember(destination => destination.UserId, options => options.MapFrom(source => source.UserId.Value));

        CreateMap<UserCreatedDomainEvent, IntegrationEventEnvelope<UserCreatedIntegrationEvent>>()
            .ConstructUsing((source, context) => new IntegrationEventEnvelope<UserCreatedIntegrationEvent>(
                context.Mapper.Map<UserCreatedIntegrationEvent>(source),
                source.UserId.Value.ToString()))
            .ForMember(destination => destination.Headers, options => options.Ignore());

        CreateMap<AccountConfirmedDomainEvent, IntegrationEventEnvelope<UserConfirmedIntegrationEvent>>()
            .ConstructUsing((source, context) => new IntegrationEventEnvelope<UserConfirmedIntegrationEvent>(
                context.Mapper.Map<UserConfirmedIntegrationEvent>(source),
                source.UserId.Value.ToString()))
            .ForMember(destination => destination.Headers, options => options.Ignore());

        CreateMap<EmailVerificationRequestedDomainEvent, IntegrationEventEnvelope<EmailVerificationRequestIntegrationEvent>>()
            .ConstructUsing((source, context) => new IntegrationEventEnvelope<EmailVerificationRequestIntegrationEvent>(
                context.Mapper.Map<EmailVerificationRequestIntegrationEvent>(source),
                source.UserId.Value.ToString()))
            .ForMember(destination => destination.Headers, options => options.Ignore());
    }
}
