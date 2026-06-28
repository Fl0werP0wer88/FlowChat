using FlowChat.Core.Exceptions;
using FlowChat.Core.Messaging;
using FlowChat.Core.Messaging.SocialGraphService.ReadModels;
using FlowChat.PresenceService.Application.Features.ContactObserverProjections;
using FlowChat.Shared.Consumers.ProjectionBulk;

namespace FlowChat.PresenceService.Consumers.Kafka.Projections;

public sealed class ContactObserverProjectionValueFactory
    : IProjectionValueFactory<ContactReadModel, ContactObserverProjectionDto, (Guid ObservedUserId, Guid ObserverUserId)>
{
    private const string ProjectionSource = "social-graph-contact-events";

    public ContactObserverProjectionDto MapValue(
        ProjectionIntegrationEvent<ContactReadModel> message) =>
        new()
        {
            ObservedUserId = ResolveUserId(message.Value.ContactUserId, nameof(message.Value.ContactUserId)),
            ObserverUserId = ResolveUserId(message.Value.OwnerUserId, nameof(message.Value.OwnerUserId)),
            Source = ProjectionSource
        };

    public (Guid ObservedUserId, Guid ObserverUserId) GetDeduplicationKey(ContactObserverProjectionDto value) =>
        (value.ObservedUserId, value.ObserverUserId);

    private static Guid ResolveUserId(Guid userId, string fieldName) =>
        userId != Guid.Empty
            ? userId
            : throw new NonTransientException($"Payload does not contain valid {fieldName}.");
}
