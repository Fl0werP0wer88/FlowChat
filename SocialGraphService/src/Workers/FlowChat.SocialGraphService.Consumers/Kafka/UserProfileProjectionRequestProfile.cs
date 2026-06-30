using AutoMapper;
using FlowChat.Core.Exceptions;
using FlowChat.Core.Messaging.UserProfileService.Events;
using FlowChat.Core.Messaging.UserProfileService.ReadModels;
using FlowChat.SocialGraphService.Application.Features.UserProfile;

namespace FlowChat.SocialGraphService.Consumers.Kafka;

public sealed class UserProfileProjectionRequestProfile : Profile
{
    private const string ProjectionSource = "user-profile-projection";

    public UserProfileProjectionRequestProfile()
    {
        CreateMap<UserProfileReadModel, UserProfileProjectionDto>()
            .ForMember(destination => destination.UserProfileId, options => options.MapFrom(source => ResolveUserProfileId(source.UserProfileId)))
            .ForMember(destination => destination.FriendlyUserId, options => options.MapFrom(source => NormalizeRequired(source.FriendlyUserId, nameof(source.FriendlyUserId))))
            .ForMember(destination => destination.FirstName, options => options.MapFrom(source => NormalizeOptional(source.FirstName)))
            .ForMember(destination => destination.LastName, options => options.MapFrom(source => NormalizeOptional(source.LastName)))
            .ForMember(destination => destination.Organization, options => options.MapFrom(source => NormalizeOptional(source.Organization)))
            .ForMember(destination => destination.MainEmail, options => options.MapFrom(source => MapEmail(source.MainEmail)))
            .ForMember(destination => destination.MainPhone, options => options.MapFrom(source => MapPhone(source.MainPhone)))
            .ForMember(destination => destination.AvatarUrl, options => options.MapFrom(source => NormalizeOptional(source.AvatarUrl)))
            .ForMember(destination => destination.Bio, options => options.MapFrom(source => NormalizeOptional(source.Bio)))
            .ForMember(destination => destination.SourceVersion, options => options.MapFrom((_, _, _, context) => (int)context.Items[nameof(UserProfileProjectionDto.SourceVersion)]))
            .ForMember(destination => destination.Source, options => options.MapFrom(_ => ProjectionSource));
    }

    private static Guid ResolveUserProfileId(Guid userProfileId) =>
        userProfileId != Guid.Empty
            ? userProfileId
            : throw new NonTransientException("Payload does not contain valid UserProfileId.");

    private static string NormalizeRequired(string? value, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new NonTransientException($"Payload does not contain valid {fieldName}.");

        return value.Trim();
    }

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static UserProfileProjectionEmailDto? MapEmail(UserProfileEmail? email)
    {
        var address = NormalizeOptional(email?.Address);
        return email is null || address is null
            ? null
            : new UserProfileProjectionEmailDto
            {
                Address = address,
                IsConfirmed = email.IsConfirmed,
                IsVisible = email.IsVisible
            };
    }

    private static UserProfileProjectionPhoneDto? MapPhone(UserProfilePhone? phone)
    {
        var number = NormalizeOptional(phone?.Number);
        return phone is null || number is null
            ? null
            : new UserProfileProjectionPhoneDto
            {
                Number = number,
                IsConfirmed = phone.IsConfirmed,
                IsVisible = phone.IsVisible
            };
    }
}
