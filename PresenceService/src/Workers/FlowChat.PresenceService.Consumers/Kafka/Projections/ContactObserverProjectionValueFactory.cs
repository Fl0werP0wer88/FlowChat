using FlowChat.Core.Exceptions;
using FlowChat.Core.Messaging;
using FlowChat.Core.Messaging.ChatService.ReadModels;
using FlowChat.PresenceService.Application.Features.ContactObserverProjections;
using FlowChat.Shared.Consumers.ProjectionBulk;

namespace FlowChat.PresenceService.Consumers.Kafka.Projections;

public sealed class ContactObserverProjectionValueFactory
    : IProjectionValueFactory<DuetConversationReadModel, ContactObserverProjectionDto, (Guid ObservedUserId, Guid ObserverUserId)>
{
    private const string ProjectionSource = "chat-duet-conversation-events";

    public ContactObserverProjectionDto MapValue(ProjectionIntegrationEvent<DuetConversationReadModel> message) =>
        MapValues(message).First();

    public IEnumerable<ContactObserverProjectionDto> MapValues(
        ProjectionIntegrationEvent<DuetConversationReadModel> message)
    {
        var firstUserId = ResolveUserId(message.Value.FirstUserId, nameof(message.Value.FirstUserId));
        var secondUserId = ResolveUserId(message.Value.SecondUserId, nameof(message.Value.SecondUserId));

        yield return new ContactObserverProjectionDto
        {
            ObserverUserId = firstUserId,
            ObservedUserId = secondUserId,
            IsBlocked = message.Value.FirstUserBlockedSecondUser,
            Source = ProjectionSource
        };

        yield return new ContactObserverProjectionDto
        {
            ObserverUserId = secondUserId,
            ObservedUserId = firstUserId,
            IsBlocked = message.Value.SecondUserBlockedFirstUser,
            Source = ProjectionSource
        };
    }

    public (Guid ObservedUserId, Guid ObserverUserId) GetDeduplicationKey(ContactObserverProjectionDto value) =>
        (value.ObservedUserId, value.ObserverUserId);

    private static Guid ResolveUserId(Guid userId, string fieldName) =>
        userId != Guid.Empty
            ? userId
            : throw new NonTransientException($"Payload does not contain valid {fieldName}.");
}
