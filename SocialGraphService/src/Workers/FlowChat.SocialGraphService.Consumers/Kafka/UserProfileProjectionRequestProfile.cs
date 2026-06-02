using AutoMapper;
using FlowChat.Core.Exceptions;
using FlowChat.Core.Messaging.UserProfileService.ReadModels;
using FlowChat.SocialGraphService.Consumers.SocialGraph.Contracts;

namespace FlowChat.SocialGraphService.Consumers.Kafka;

public sealed class UserProfileProjectionRequestProfile : Profile
{
    private const string ProjectionSource = "user-profile-projection";

    public UserProfileProjectionRequestProfile()
    {
        CreateMap<UserProfileReadModel, UserProfileProjectionRequest>()
            .ForMember(destination => destination.UserProfileId, options => options.MapFrom(source => ResolveUserProfileId(source.UserProfileId)))
            .ForMember(destination => destination.FriendlyUserId, options => options.MapFrom(source => NormalizeRequired(source.FriendlyUserId, nameof(source.FriendlyUserId))))
            .ForMember(destination => destination.FirstName, options => options.MapFrom(source => NormalizeOptional(source.FirstName)))
            .ForMember(destination => destination.LastName, options => options.MapFrom(source => NormalizeOptional(source.LastName)))
            .ForMember(destination => destination.Organization, options => options.MapFrom(source => NormalizeOptional(source.Organization)))
            .ForMember(destination => destination.MainEmailAddress, options => options.MapFrom(source => NormalizeOptional(source.MainEmail == null ? null : source.MainEmail.Address)))
            .ForMember(destination => destination.MainEmailIsConfirmed, options => options.MapFrom(source => source.MainEmail == null ? null : (bool?)source.MainEmail.IsConfirmed))
            .ForMember(destination => destination.MainEmailIsVisible, options => options.MapFrom(source => source.MainEmail == null ? null : (bool?)source.MainEmail.IsVisible))
            .ForMember(destination => destination.MainPhoneNumber, options => options.MapFrom(source => NormalizeOptional(source.MainPhone == null ? null : source.MainPhone.Number)))
            .ForMember(destination => destination.MainPhoneIsConfirmed, options => options.MapFrom(source => source.MainPhone == null ? null : (bool?)source.MainPhone.IsConfirmed))
            .ForMember(destination => destination.MainPhoneIsVisible, options => options.MapFrom(source => source.MainPhone == null ? null : (bool?)source.MainPhone.IsVisible))
            .ForMember(destination => destination.AvatarUrl, options => options.MapFrom(source => NormalizeOptional(source.AvatarUrl)))
            .ForMember(destination => destination.Bio, options => options.MapFrom(source => NormalizeOptional(source.Bio)))
            .ForMember(destination => destination.SourceVersion, options => options.Ignore())
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
}
