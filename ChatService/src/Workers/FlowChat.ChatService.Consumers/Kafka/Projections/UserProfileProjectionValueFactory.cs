using AutoMapper;
using FlowChat.ChatService.Application.Features.UserProfile;
using FlowChat.Core.Exceptions;
using FlowChat.Core.Messaging;
using FlowChat.Core.Messaging.UserProfileService.ReadModels;
using FlowChat.Shared.Consumers.Projections.Single;

namespace FlowChat.ChatService.Consumers.Kafka.Projections;

public sealed class UserProfileProjectionValueFactory(IMapper mapper)
    : IProjectionSingleValueFactory<UserProfileReadModel, UserProfileProjectionDto>
{
    private const string ProjectionSource = "user-profile-projection";

    public UserProfileProjectionDto MapValue(
        ProjectionIntegrationEvent<UserProfileReadModel> message)
    {
        if (message.Operation == OperationType.Deleted)
        {
            return new UserProfileProjectionDto
            {
                UserProfileId = ResolveUserId(message.SourceAggregateId, nameof(message.SourceAggregateId)),
                FriendlyUserId = string.Empty,
                Source = ProjectionSource
            };
        }

        try
        {
            return mapper.Map<UserProfileProjectionDto>(message.Value);
        }
        catch (AutoMapperMappingException exception) when (exception.InnerException is NonTransientException nonTransientException)
        {
            throw nonTransientException;
        }
    }

    private static Guid ResolveUserId(Guid userId, string fieldName) =>
        userId != Guid.Empty
            ? userId
            : throw new NonTransientException($"Payload does not contain valid {fieldName}.");
}
