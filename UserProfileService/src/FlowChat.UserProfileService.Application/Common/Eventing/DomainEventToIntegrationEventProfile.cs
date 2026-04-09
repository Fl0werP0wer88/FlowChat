using AutoMapper;
using FlowChat.Shared.Domain;
using FlowChat.Core.Messaging.UserProfileService.Events;
using FlowChat.UserProfileService.Domain.Entities.UserProfile;
using FlowChat.UserProfileService.Domain.Entities.UserProfile.Events;

namespace FlowChat.UserProfileService.Application.Common.Eventing;

public sealed class DomainEventToIntegrationEventProfile : Profile
{
    public DomainEventToIntegrationEventProfile()
    {
        CreateMap<UserProfileCreatedDomainEvent, UserProfileCreatedIntegrationEvent>()
            .ForMember(destination => destination.Key, options => options.MapFrom(source => source.UserProfileId.Value.ToString()))
            .ForMember(destination => destination.UserProfileId, options => options.MapFrom(source => source.UserProfileId.Value))
            .ForMember(destination => destination.FriendlyUserId, options => options.MapFrom(source => source.FriendlyUserId))
            .ForMember(destination => destination.FirstName, options => options.MapFrom(source => source.FirstName))
            .ForMember(destination => destination.LastName, options => options.MapFrom(source => source.LastName))
            .ForMember(destination => destination.Organization, options => options.MapFrom(source => source.Organization))
            .ForMember(destination => destination.MainEmail, options => options.MapFrom(source => new UserProfileEmail
            {
                Id = source.MainEmailId.Value,
                UserProfileId = source.UserProfileId.Value,
                Address = source.MainEmail.Value,
                IsMain = true,
                IsAuth = true,
                IsConfirmed = false,
                IsVisible = true
            }))
            .ForMember(destination => destination.LastSeenAtUtc, options => options.MapFrom(source => source.LastSeenAtUtc == null ? (DateTimeOffset?)null : source.LastSeenAtUtc.Value))
            .ForMember(destination => destination.MainPhone, options => options.MapFrom(source => source.MainPhone == null
                ? null
                : new UserProfilePhone
                {
                    Id = source.MainPhoneId == null ? Guid.Empty : source.MainPhoneId.Value,
                    UserProfileId = source.UserProfileId.Value,
                    Number = source.MainPhone.Value,
                    IsMain = true,
                    IsConfirmed = false,
                    IsVisible = true
                }));

        CreateMap<EmailConfirmedDomainEvent, UserEmailConfirmedIntegrationEvent>()
            .ForMember(destination => destination.Key, options => options.MapFrom(source => source.UserProfileId.Value.ToString()))
            .ForMember(destination => destination.UserProfileId, options => options.MapFrom(source => source.UserProfileId.Value))
            .ForMember(destination => destination.EmailId, options => options.MapFrom(source => source.EmailId.Value))
            .ForMember(destination => destination.Email, options => options.MapFrom(source => new FlowChat.Core.Messaging.UserProfileService.Events.Email
            {
                Address = source.Email.Value,
                IsAuth = source.IsAuth
            }));

        CreateMap<AggregateStateChangedDomainEvent<UserProfile, UserProfileState>, UserProfileChangedIntegrationEvent>()
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
                Id = mainEmail.Id,
                UserProfileId = mainEmail.UserProfileId,
                Address = mainEmail.Address,
                IsMain = mainEmail.IsMain,
                IsAuth = mainEmail.IsAuth,
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
                Id = mainPhone.Id,
                UserProfileId = mainPhone.UserProfileId,
                Number = mainPhone.Number,
                IsMain = mainPhone.IsMain,
                IsConfirmed = mainPhone.IsConfirmed,
                IsVisible = mainPhone.IsVisible
            };
    }
}
