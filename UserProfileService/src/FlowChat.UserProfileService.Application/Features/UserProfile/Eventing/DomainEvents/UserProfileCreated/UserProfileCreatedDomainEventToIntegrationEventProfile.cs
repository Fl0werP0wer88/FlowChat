using AutoMapper;
using FlowChat.Core.Messaging.UserProfileService.Events;
using FlowChat.UserProfileService.Domain.Entities.UserProfile.Events;

namespace FlowChat.UserProfileService.Application.Features.UserProfile.Eventing.DomainEvents.UserProfileCreated;

public sealed class UserProfileCreatedDomainEventToIntegrationEventProfile : Profile
{
    public UserProfileCreatedDomainEventToIntegrationEventProfile()
    {
        CreateMap<UserProfileCreatedDomainEvent, UserProfileCreatedIntegrationEvent>()
            .ForMember(destination => destination.UserProfileId, options => options.MapFrom(source => source.UserProfileId.Value))
            .ForMember(destination => destination.FriendlyUserId, options => options.MapFrom(source => source.FriendlyUserId))
            .ForMember(destination => destination.FirstName, options => options.MapFrom(source => source.FirstName))
            .ForMember(destination => destination.LastName, options => options.MapFrom(source => source.LastName))
            .ForMember(destination => destination.Organization, options => options.MapFrom(source => source.Organization))
            .ForMember(destination => destination.MainEmail, options => options.MapFrom(source => new UserProfileEmail
            {
                Address = source.MainEmail.Value,
                IsConfirmed = false,
                IsVisible = true
            }))
            .ForMember(destination => destination.LastSeenAtUtc, options => options.MapFrom(source => source.LastSeenAtUtc == null ? (DateTimeOffset?)null : source.LastSeenAtUtc.Value))
            .ForMember(destination => destination.MainPhone, options => options.MapFrom(source => source.MainPhone == null
                ? null
                : new UserProfilePhone
                {
                    Number = source.MainPhone.Value,
                    IsConfirmed = false,
                    IsVisible = true
                }));
    }
}
