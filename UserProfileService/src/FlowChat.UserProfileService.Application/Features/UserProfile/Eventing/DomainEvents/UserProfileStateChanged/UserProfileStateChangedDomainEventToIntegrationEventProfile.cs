using AutoMapper;
using FlowChat.Core.Messaging.UserProfileService.Events;
using FlowChat.Shared.Domain;
using FlowChat.UserProfileService.Domain.Entities.UserProfile;
using DomainUserProfile = FlowChat.UserProfileService.Domain.Entities.UserProfile.UserProfile;

namespace FlowChat.UserProfileService.Application.Features.UserProfile.Eventing.DomainEvents.UserProfileStateChanged;

public sealed class UserProfileStateChangedDomainEventToIntegrationEventProfile : Profile
{
    public UserProfileStateChangedDomainEventToIntegrationEventProfile()
    {
        CreateMap<AggregateStateChangedDomainEvent<DomainUserProfile, UserProfileState>, UserProfileChangedIntegrationEvent>()
            .ForMember(destination => destination.Key, options => options.MapFrom(source => source.AggregateId.ToString()))
            .ForMember(destination => destination.UserProfileId, options => options.MapFrom(source => source.AggregateState.Id))
            .ForMember(destination => destination.FriendlyUserId, options => options.MapFrom(source => source.AggregateState.FriendlyUserId))
            .ForMember(destination => destination.FirstName, options => options.MapFrom(source => source.AggregateState.FirstName))
            .ForMember(destination => destination.LastName, options => options.MapFrom(source => source.AggregateState.LastName))
            .ForMember(destination => destination.Organization, options => options.MapFrom(source => source.AggregateState.Organization))
            .ForMember(destination => destination.MainEmail, options => options.MapFrom(source => GetMainEmail(source.AggregateState)))
            .ForMember(destination => destination.MainPhone, options => options.MapFrom(source => GetMainPhone(source.AggregateState)))
            .ForMember(destination => destination.AvatarUrl, options => options.MapFrom(source => source.AggregateState.AvatarUrl))
            .ForMember(destination => destination.Bio, options => options.MapFrom(source => source.AggregateState.Bio))
            .ForMember(destination => destination.IsActive, options => options.MapFrom(source => source.AggregateState.IsActive))
            .ForMember(destination => destination.LastSeenAtUtc, options => options.MapFrom(source => source.AggregateState.LastSeenAtUtc == null ? (DateTimeOffset?)null : source.AggregateState.LastSeenAtUtc.Value));
    }

    private static UserProfileEmail? GetMainEmail(UserProfileState state)
    {
        var mainEmail = state.Emails.FirstOrDefault(email => email.IsMain);
        return mainEmail == null
            ? null
            : new UserProfileEmail
            {
                Address = mainEmail.Address,
                IsConfirmed = mainEmail.IsConfirmed,
                IsVisible = mainEmail.IsVisible
            };
    }

    private static UserProfilePhone? GetMainPhone(UserProfileState state)
    {
        var mainPhone = state.Phones.FirstOrDefault(phone => phone.IsMain);
        return mainPhone == null
            ? null
            : new UserProfilePhone
            {
                Number = mainPhone.Number,
                IsConfirmed = mainPhone.IsConfirmed,
                IsVisible = mainPhone.IsVisible
            };
    }
}
