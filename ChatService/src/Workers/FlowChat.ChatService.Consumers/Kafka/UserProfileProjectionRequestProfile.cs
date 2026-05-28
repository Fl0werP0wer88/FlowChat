using AutoMapper;
using FlowChat.ChatService.Consumers.ChatService.Contracts;
using FlowChat.Core.Exceptions;
using FlowChat.Core.Messaging.UserProfileService.Events;

namespace FlowChat.ChatService.Consumers.Kafka;

public sealed class UserProfileProjectionRequestProfile : Profile
{
    private const string ProjectionSource = "user-profile-events";

    public UserProfileProjectionRequestProfile()
    {
        CreateMap<UserProfileCreatedIntegrationEvent, UserProfileProjectionRequest>()
            .ForMember(destination => destination.UserProfileId, options => options.MapFrom(source => ResolveUserId(source.UserProfileId)))
            .ForMember(destination => destination.FriendlyUserId, options => options.MapFrom(source => NormalizeRequired(source.FriendlyUserId, nameof(source.FriendlyUserId))))
            .ForMember(destination => destination.DisplayName, options => options.MapFrom(source => ComputeDisplayName(source.FirstName, source.LastName)))
            .ForMember(destination => destination.AvatarUrl, options => options.MapFrom(source => NormalizeOptional(source.AvatarUrl)))
            .ForMember(destination => destination.Source, options => options.MapFrom(_ => ProjectionSource));

        CreateMap<UserProfileChangedIntegrationEvent, UserProfileProjectionRequest>()
            .ForMember(destination => destination.UserProfileId, options => options.MapFrom(source => ResolveUserId(source.UserProfileId)))
            .ForMember(destination => destination.FriendlyUserId, options => options.MapFrom(source => NormalizeRequired(source.FriendlyUserId, nameof(source.FriendlyUserId))))
            .ForMember(destination => destination.DisplayName, options => options.MapFrom(source => ComputeDisplayName(source.FirstName, source.LastName)))
            .ForMember(destination => destination.AvatarUrl, options => options.MapFrom(source => NormalizeOptional(source.AvatarUrl)))
            .ForMember(destination => destination.Source, options => options.MapFrom(_ => ProjectionSource));
    }

    private static Guid ResolveUserId(Guid userId) =>
        userId != Guid.Empty
            ? userId
            : throw new NonTransientException("Payload does not contain valid UserProfileId.");

    private static string NormalizeRequired(string? value, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new NonTransientException($"Payload does not contain valid {fieldName}.");

        return value.Trim();
    }

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string? ComputeDisplayName(string? firstName, string? lastName)
    {
        var parts = new[] { firstName?.Trim(), lastName?.Trim() }
            .Where(p => !string.IsNullOrEmpty(p));
        var name = string.Join(" ", parts);
        return string.IsNullOrEmpty(name) ? null : name;
    }
}
