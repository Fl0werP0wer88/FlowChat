using AutoMapper;
using FlowChat.Core.Messaging.UserProfileService.Events;
using FlowChat.Core.Messaging.UserProfileService.ReadModels;
using DomainUserProfile = FlowChat.UserProfileService.Domain.Entities.UserProfile.UserProfile;

namespace FlowChat.UserProfileService.Application.Features.UserProfile.Mapping;

public sealed class UserProfileReadModelProfile : Profile
{
    public UserProfileReadModelProfile()
    {
        CreateMap<DomainUserProfile, UserProfileReadModel>()
            .ForMember(destination => destination.UserProfileId, options => options.MapFrom(source => source.Id.Value))
            .ForMember(destination => destination.FriendlyUserId, options => options.MapFrom(source => source.FriendlyUserId.Value))
            .ForMember(destination => destination.MainEmail, options => options.MapFrom(source => GetMainEmail(source)))
            .ForMember(destination => destination.MainPhone, options => options.MapFrom(source => GetMainPhone(source)))
            .ForMember(destination => destination.LastSeenAtUtc, options => options.MapFrom(source => source.LastSeenAtUtc == null ? (DateTimeOffset?)null : source.LastSeenAtUtc.Value));
    }

    private static UserProfileEmail? GetMainEmail(DomainUserProfile userProfile)
    {
        var mainEmail = userProfile.Emails.FirstOrDefault(email => email.IsMain);
        return mainEmail is null
            ? null
            : new UserProfileEmail
            {
                Address = mainEmail.Address.Value,
                IsConfirmed = mainEmail.IsConfirmed,
                IsVisible = mainEmail.IsVisible
            };
    }

    private static UserProfilePhone? GetMainPhone(DomainUserProfile userProfile)
    {
        var mainPhone = userProfile.Phones.FirstOrDefault(phone => phone.IsMain);
        return mainPhone is null
            ? null
            : new UserProfilePhone
            {
                Number = mainPhone.Number.Value,
                IsConfirmed = mainPhone.IsConfirmed,
                IsVisible = mainPhone.IsVisible
            };
    }
}
