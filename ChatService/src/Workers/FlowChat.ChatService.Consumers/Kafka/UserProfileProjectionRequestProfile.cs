using AutoMapper;
using FlowChat.ChatService.Consumers.ChatService.Contracts;
using FlowChat.Core.Exceptions;
using FlowChat.Core.Messaging.UserProfileService.ReadModels;

namespace FlowChat.ChatService.Consumers.Kafka;

public sealed class UserProfileProjectionRequestProfile : Profile
{
    private const string ProjectionSource = "user-profile-projection";

    public UserProfileProjectionRequestProfile()
    {
        CreateMap<UserProfileReadModel, UserProfileProjectionRequest>()
            .ForMember(destination => destination.UserProfileId, options => options.MapFrom(source => ResolveUserId(source.UserProfileId)))
            .ForMember(destination => destination.FriendlyUserId, options => options.MapFrom(source => NormalizeRequired(source.FriendlyUserId, nameof(source.FriendlyUserId))))
            .ForMember(destination => destination.FirstName, options => options.MapFrom(source => NormalizeOptional(source.FirstName)))
            .ForMember(destination => destination.LastName, options => options.MapFrom(source => NormalizeOptional(source.LastName)))
            .ForMember(destination => destination.AvatarUrl, options => options.MapFrom(source => NormalizeOptional(source.AvatarUrl)))
            .ForMember(destination => destination.SourceVersion, options => options.Ignore())
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
}
