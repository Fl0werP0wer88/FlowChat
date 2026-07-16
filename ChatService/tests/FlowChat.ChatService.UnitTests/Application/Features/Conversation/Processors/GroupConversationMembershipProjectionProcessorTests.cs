using AutoMapper;
using FlowChat.ChatService.Application.Features.Conversation.Mapping;
using FlowChat.ChatService.Application.Features.Conversation.Processors;
using FlowChat.ChatService.Domain.Entities.Conversation;
using FlowChat.Core.Messaging;
using FlowChat.Core.Messaging.ChatService.ReadModels;
using FlowChat.Shared.Application;
using FlowChat.Shared.Domain;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using ConversationAggregate = FlowChat.ChatService.Domain.Entities.Conversation.Conversation;
using UserProfileMarker = FlowChat.ChatService.Domain.Entities.UserProfiles.UserProfile;

namespace FlowChat.ChatService.UnitTests.Application.Features.Conversation.Processors;

public sealed class GroupConversationMembershipProjectionProcessorTests
{
    private static readonly IMapper Mapper = new MapperConfiguration(
        cfg => cfg.AddProfile<GroupConversationMembershipReadModelProfile>(),
        NullLoggerFactory.Instance).CreateMapper();

    [Fact]
    public async Task ProcessAsync_WhenGroupCreated_PublishesAllMembershipsAsAddedAtInitialRevision()
    {
        var conversation = CreateConversation();
        var published = new List<IntegrationEventEnvelope<DeltaProjectionIntegrationEvent<GroupConversationMembershipReadModel>>>();
        var processor = CreateProcessor(published);

        await processor.ProcessAsync(new TestCommand(), conversation, MutationType.Created, CancellationToken.None);

        var payload = published.Should().ContainSingle().Which.Payload;
        payload.Operation.Should().Be(DeltaOperationType.Added);
        payload.ProjectionRevision.Should().Be(1);
        payload.Value.Select(value => value.ParticipantUserId)
            .Should().BeEquivalentTo(conversation.Participants.Select(participant => participant.UserId.Value));
    }

    [Fact]
    public async Task ProcessAsync_WhenParticipantAdded_PublishesOnlyAddedMembershipAtCurrentRevision()
    {
        var conversation = CreateConversation();
        var newMemberId = Id<UserProfileMarker>.New();
        var published = new List<IntegrationEventEnvelope<DeltaProjectionIntegrationEvent<GroupConversationMembershipReadModel>>>();
        var processor = CreateProcessor(published);
        processor.CaptureBeforeState(conversation);

        conversation.AddParticipants([newMemberId]);
        await processor.ProcessAsync(new TestCommand(), conversation, MutationType.Updated, CancellationToken.None);

        var payload = published.Should().ContainSingle().Which.Payload;
        payload.Operation.Should().Be(DeltaOperationType.Added);
        payload.ProjectionRevision.Should().Be(2);
        payload.Value.Should().ContainSingle().Which.ParticipantUserId.Should().Be(newMemberId.Value);
    }

    [Fact]
    public async Task ProcessAsync_WhenParticipantRemoved_PublishesOnlyRemovedMembershipAtCurrentRevision()
    {
        var conversation = CreateConversation(additionalParticipant: true);
        var removedMemberId = conversation.Participants.Last().UserId;
        var published = new List<IntegrationEventEnvelope<DeltaProjectionIntegrationEvent<GroupConversationMembershipReadModel>>>();
        var processor = CreateProcessor(published);
        processor.CaptureBeforeState(conversation);

        conversation.RemoveParticipants([removedMemberId]);
        await processor.ProcessAsync(new TestCommand(), conversation, MutationType.Updated, CancellationToken.None);

        var payload = published.Should().ContainSingle().Which.Payload;
        payload.Operation.Should().Be(DeltaOperationType.Removed);
        payload.ProjectionRevision.Should().Be(2);
        payload.Value.Should().ContainSingle().Which.ParticipantUserId.Should().Be(removedMemberId.Value);
    }

    [Fact]
    public async Task ProcessAsync_WhenMembershipDidNotChange_DoesNotPublishDelta()
    {
        var conversation = CreateConversation();
        var published = new List<IntegrationEventEnvelope<DeltaProjectionIntegrationEvent<GroupConversationMembershipReadModel>>>();
        var processor = CreateProcessor(published);
        processor.CaptureBeforeState(conversation);

        await processor.ProcessAsync(new TestCommand(), conversation, MutationType.Updated, CancellationToken.None);

        published.Should().BeEmpty();
    }

    private static GroupConversationMembershipProjectionProcessor<TestCommand> CreateProcessor(
        ICollection<IntegrationEventEnvelope<DeltaProjectionIntegrationEvent<GroupConversationMembershipReadModel>>> published)
    {
        var publisher = new Mock<IOutboxIntegrationEventPublisher>();
        publisher.Setup(instance => instance.PublishAsync(
                It.IsAny<IntegrationEventEnvelope<DeltaProjectionIntegrationEvent<GroupConversationMembershipReadModel>>>(),
                It.IsAny<CancellationToken>()))
            .Callback<IntegrationEventEnvelope<DeltaProjectionIntegrationEvent<GroupConversationMembershipReadModel>>, CancellationToken>(
                (envelope, _) => published.Add(envelope))
            .Returns(Task.CompletedTask);

        return new GroupConversationMembershipProjectionProcessor<TestCommand>(
            Mapper,
            publisher.Object,
            new GroupConversationMembershipRevisionProvider());
    }

    private static GroupConversation CreateConversation(bool additionalParticipant = false)
    {
        var creatorId = Id<UserProfileMarker>.New();
        var participantIds = new List<Id<UserProfileMarker>> { creatorId, Id<UserProfileMarker>.New() };
        if (additionalParticipant)
        {
            participantIds.Add(Id<UserProfileMarker>.New());
        }

        var conversation = GroupConversation.Create(
            Id<ConversationAggregate>.New(),
            creatorId,
            participantIds,
            "Dev Team");
        conversation.SetCreated("test");
        conversation.SetUpdated("test");
        return conversation;
    }

    private sealed record TestCommand;
}
