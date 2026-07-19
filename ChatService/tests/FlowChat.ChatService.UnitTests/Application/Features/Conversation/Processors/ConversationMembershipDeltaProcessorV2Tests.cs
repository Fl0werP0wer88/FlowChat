using FlowChat.ChatService.Application.Features.Conversation.Commands.AddGroupParticipants;
using FlowChat.ChatService.Application.Features.Conversation.Commands.RemoveGroupParticipants;
using FlowChat.ChatService.Application.Features.Conversation.Processors;
using FlowChat.ChatService.Domain.Entities.Conversation;
using FlowChat.ChatService.Domain.Entities.Conversation.Events;
using FlowChat.Core.Messaging;
using FlowChat.Core.Messaging.ChatService.ReadModels;
using FlowChat.Shared.Application;
using FlowChat.Shared.Domain;
using FluentAssertions;
using Moq;
using UserProfileMarker = FlowChat.ChatService.Domain.Entities.UserProfiles.UserProfile;

namespace FlowChat.ChatService.UnitTests.Application.Features.Conversation.Processors;

public sealed class ConversationMembershipDeltaProcessorV2Tests
{
    [Fact]
    public async Task ProcessAsync_WhenMembershipCreated_PublishesSingleAddedDeltaAtVersionTwo()
    {
        var conversationId = Id<ConversationV2>.New();
        var participantIds = new[] { Id<UserProfileMarker>.New(), Id<UserProfileMarker>.New() };
        var membership = ConversationMembership.Create(conversationId, ConversationType.Group, participantIds);
        PreparePersistedMutation(membership);
        var domainEvent = new ConversationCreatedDomainEventV2(
            conversationId,
            ConversationType.Group,
            "Team",
            participantIds[0],
            participantIds);
        var published = new List<IntegrationEventEnvelope<DeltaProjectionIntegrationEvent<ConversationMembershipReadModelV2>>>();
        var processor = new CreateConversationMembershipDeltaProcessorV2(CreatePublisher(published));

        await processor.ProcessAsync(domainEvent, membership, MutationType.Created, CancellationToken.None);

        var envelope = published.Should().ContainSingle().Which;
        envelope.KafkaKey.Should().Be(conversationId.Value.ToString("D"));
        envelope.Payload.Operation.Should().Be(DeltaOperationType.Added);
        envelope.Payload.SourceAggregateVersion.Should().Be(2);
        envelope.Payload.ProjectionRevision.Should().Be(2);
        envelope.Payload.Value.Select(x => x.ParticipantUserId)
            .Should().BeEquivalentTo(participantIds.Select(x => x.Value));
    }

    [Fact]
    public async Task ProcessAsync_WhenParticipantsAdded_PublishesOneDeltaWithAllCommandUsers()
    {
        var initialIds = new[] { Id<UserProfileMarker>.New(), Id<UserProfileMarker>.New() };
        var membership = ConversationMembership.Create(
            Id<ConversationV2>.New(),
            ConversationType.Group,
            initialIds);
        membership.SetCreated("test");
        var addedIds = new[] { Guid.NewGuid(), Guid.NewGuid() };
        membership.AddParticipants(addedIds.Select(Id<UserProfileMarker>.FromGuid), 7);
        membership.IncrementVersion();
        membership.SetUpdated("test");
        var published = new List<IntegrationEventEnvelope<DeltaProjectionIntegrationEvent<ConversationMembershipReadModelV2>>>();
        var processor = new AddConversationMembershipDeltaProcessorV2(CreatePublisher(published));

        await processor.ProcessAsync(
            new AddGroupParticipantsCommandV2(membership.ConversationId.Value, addedIds),
            membership,
            MutationType.Updated,
            CancellationToken.None);

        var payload = published.Should().ContainSingle().Which.Payload;
        payload.Operation.Should().Be(DeltaOperationType.Added);
        payload.Value.Select(x => x.ParticipantUserId).Should().BeEquivalentTo(addedIds);
        payload.ProjectionRevision.Should().Be(membership.Version);
    }

    [Fact]
    public async Task ProcessAsync_WhenParticipantsRemoved_PublishesOneRemovedDelta()
    {
        var ids = new[]
        {
            Id<UserProfileMarker>.New(),
            Id<UserProfileMarker>.New(),
            Id<UserProfileMarker>.New()
        };
        var membership = ConversationMembership.Create(
            Id<ConversationV2>.New(),
            ConversationType.Group,
            ids);
        membership.SetCreated("test");
        membership.RemoveParticipants([ids[2]]);
        membership.IncrementVersion();
        membership.SetUpdated("test");
        var published = new List<IntegrationEventEnvelope<DeltaProjectionIntegrationEvent<ConversationMembershipReadModelV2>>>();
        var processor = new RemoveConversationMembershipDeltaProcessorV2(CreatePublisher(published));

        await processor.ProcessAsync(
            new RemoveGroupParticipantsCommandV2(membership.ConversationId.Value, [ids[2].Value]),
            membership,
            MutationType.Updated,
            CancellationToken.None);

        var payload = published.Should().ContainSingle().Which.Payload;
        payload.Operation.Should().Be(DeltaOperationType.Removed);
        payload.Value.Should().ContainSingle().Which.ParticipantUserId.Should().Be(ids[2].Value);
    }

    private static ConversationMembershipDeltaPublisherV2 CreatePublisher(
        ICollection<IntegrationEventEnvelope<DeltaProjectionIntegrationEvent<ConversationMembershipReadModelV2>>> published)
    {
        var publisher = new Mock<IOutboxIntegrationEventPublisher>();
        publisher.Setup(x => x.PublishAsync(
                It.IsAny<IntegrationEventEnvelope<DeltaProjectionIntegrationEvent<ConversationMembershipReadModelV2>>>(),
                It.IsAny<CancellationToken>()))
            .Callback<IntegrationEventEnvelope<DeltaProjectionIntegrationEvent<ConversationMembershipReadModelV2>>, CancellationToken>(
                (envelope, _) => published.Add(envelope))
            .Returns(Task.CompletedTask);
        return new ConversationMembershipDeltaPublisherV2(publisher.Object);
    }

    private static void PreparePersistedMutation(ConversationMembership membership)
    {
        membership.IncrementVersion();
        membership.SetCreated("test");
        membership.SetUpdated("test");
    }
}
