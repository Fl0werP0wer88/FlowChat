using AutoMapper;
using FlowChat.Shared.Domain;
using FlowChat.Core.Messaging.UserProfileService.Events;
using FlowChat.UserProfileService.Domain.Entities;
using FlowChat.UserProfileService.Domain.Events;

namespace FlowChat.UserProfileService.Application.Common.Eventing;

public sealed class DomainEventToIntegrationEventProfile : Profile
{
    public DomainEventToIntegrationEventProfile()
    {
        CreateMap<UserProfileCreatedDomainEvent, UserProfileCreatedIntegrationEvent>()
            .ForMember(destination => destination.Key, options => options.MapFrom(source => source.UserProfileId.Value.ToString()))
            .ForMember(destination => destination.UserProfileId, options => options.MapFrom(source => source.UserProfileId.Value))
            .ForMember(destination => destination.MainEmail, options => options.MapFrom(source => source.MainEmail.Value))
            .ForMember(destination => destination.MainPhone, options => options.MapFrom(source => source.MainPhone == null ? null : source.MainPhone.Value));

        CreateMap<EmailConfirmedDomainEvent, UserEmailConfirmedIntegrationEvent>()
            .ForMember(destination => destination.Key, options => options.MapFrom(source => source.UserProfileId.Value.ToString()))
            .ForMember(destination => destination.UserProfileId, options => options.MapFrom(source => source.UserProfileId.Value))
            .ForMember(destination => destination.EmailId, options => options.MapFrom(source => source.EmailId.Value))
            .ForMember(destination => destination.Email, options => options.MapFrom(source => source.Email.Value));

        CreateMap<AggregateStateChangedDomainEvent<UserProfile, UserProfileSnapshot>, UserProfileStateChangedIntegrationEvent>()
            .ForMember(destination => destination.Key, options => options.MapFrom(source => source.AggregateId.ToString()))
            .ForMember(destination => destination.UserProfileId, options => options.MapFrom(source => source.AggregateState.UserProfileId))
            .ForMember(destination => destination.UserName, options => options.MapFrom(source => source.AggregateState.UserName))
            .ForMember(destination => destination.DisplayName, options => options.MapFrom(source => source.AggregateState.DisplayName))
            .ForMember(destination => destination.MainEmail, options => options.MapFrom(source => source.AggregateState.MainEmail))
            .ForMember(destination => destination.IsMainEmailConfirmed, options => options.MapFrom(source => source.AggregateState.IsMainEmailConfirmed))
            .ForMember(destination => destination.MainPhone, options => options.MapFrom(source => source.AggregateState.MainPhone))
            .ForMember(destination => destination.AvatarUrl, options => options.MapFrom(source => source.AggregateState.AvatarUrl))
            .ForMember(destination => destination.Bio, options => options.MapFrom(source => source.AggregateState.Bio))
            .ForMember(destination => destination.IsActive, options => options.MapFrom(source => source.AggregateState.IsActive))
            .ForMember(destination => destination.LastSeenAtUtc, options => options.MapFrom(source => source.AggregateState.LastSeenAtUtc))
            .ForMember(destination => destination.IsEmailVisible, options => options.MapFrom(source => source.AggregateState.IsEmailVisible))
            .ForMember(destination => destination.IsPhoneVisible, options => options.MapFrom(source => source.AggregateState.IsPhoneVisible));
    }
}

