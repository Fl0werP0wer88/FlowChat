using AutoMapper;
using FlowChat.Core.Exceptions;
using FlowChat.Core.Messaging;
using FlowChat.Core.Messaging.UserProfileService.ReadModels;
using FlowChat.Shared.Consumers.ProjectionBulk;
using FlowChat.SocialGraphService.Application.Features.UserProfile;

namespace FlowChat.SocialGraphService.Consumers.Kafka.Projections;

public sealed class UserProfileProjectionValueFactory(IMapper mapper)
    : IProjectionValueFactory<UserProfileReadModel, UserProfileProjectionDto, Guid>
{
    private const string ProjectionSource = "user-profile-projection";

    public UserProfileProjectionDto MapValue(
        ProjectionIntegrationEvent<UserProfileReadModel> message)
    {
        if (message.Operation == OperationType.Deleted)
        {
            return new UserProfileProjectionDto
            {
                UserProfileId = ResolveUserProfileId(message.SourceAggregateId, nameof(message.SourceAggregateId)),
                FriendlyUserId = string.Empty,
                IsActive = false,
                Source = ProjectionSource
            };
        }

        try
        {
            return mapper.Map<UserProfileProjectionDto>(
                message.Value,
                options => options.Items[nameof(UserProfileProjectionDto.SourceVersion)] = message.SourceAggregateVersion);
        }
        catch (AutoMapperMappingException exception) when (exception.InnerException is NonTransientException nonTransientException)
        {
            throw nonTransientException;
        }
    }

    public Guid GetDeduplicationKey(UserProfileProjectionDto value) =>
        value.UserProfileId;

    private static Guid ResolveUserProfileId(Guid userProfileId, string fieldName) =>
        userProfileId != Guid.Empty
            ? userProfileId
            : throw new NonTransientException($"Payload does not contain valid {fieldName}.");
}
