using FlowChat.ChatService.Application.Contracts.Persistence;
using FlowChat.ChatService.Application.Features.Conversation.Eventing.DomainEvents.ConversationParticipantAdded;
using FlowChat.ChatService.Domain.Entities.Conversation;
using FlowChat.ChatService.Domain.Entities.Conversation.Events;
using FlowChat.Core.Messaging;
using FlowChat.Shared.Application;
using FlowChat.Shared.Application.CommandHandlers.AggregateRootCommandHandlerBaseV2.BeforeSaveProcessors;
using FlowChat.Shared.Application.CommandHandlers.BatchAggregateCommandHandlerBase.BeforeSaveProcessors;
using FlowChat.Shared.Domain;
using FluentAssertions;
using Moq;
using UserProfileMarker = FlowChat.ChatService.Domain.Entities.UserProfiles.UserProfile;

namespace FlowChat.ChatService.UnitTests.Application.Features.Conversation.Eventing.DomainEvents.ConversationParticipantAdded;

public sealed class ConversationParticipantsAddedDomainEventHandlerV2Tests
{
    [Fact]
    public async Task Handle_WithParticipants_CreatesAndProcessesEveryParticipantInEventOrder()
    {
        var repository = new Mock<IConversationParticipantWriteRepository>();
        var dispatcher = new Mock<ILocalEventDispatcher>();
        var processor = new Mock<IAggregateBeforeSaveProcessorV2<
            ConversationParticipantsAddedDomainEventV2,
            ConversationParticipant>>();
        var deltaProcessor = new Mock<IAggregateBeforeSaveDeltaProcessorV2<
            ConversationParticipantsAddedDomainEventV2,
            ConversationParticipant>>();
        var addedParticipants = new List<ConversationParticipant>();
        var processedParticipants = new List<ConversationParticipant>();
        AggregateDeltaBatch<ConversationParticipant>? capturedBatch = null;
        var cancellationToken = new CancellationTokenSource().Token;

        repository.Setup(instance => instance.AddAsync(
                It.IsAny<ConversationParticipant>(),
                cancellationToken))
            .Callback<ConversationParticipant, CancellationToken>((participant, _) =>
                addedParticipants.Add(participant))
            .ReturnsAsync((ConversationParticipant participant, CancellationToken _) => participant);
        processor.Setup(instance => instance.ProcessAsync(
                It.IsAny<ConversationParticipantsAddedDomainEventV2>(),
                It.IsAny<ConversationParticipant>(),
                MutationType.Created,
                cancellationToken))
            .Callback<ConversationParticipantsAddedDomainEventV2, ConversationParticipant, MutationType, CancellationToken>(
                (_, participant, _, _) => processedParticipants.Add(participant))
            .Returns(Task.CompletedTask);
        deltaProcessor.Setup(instance => instance.ProcessAsync(
                It.IsAny<ConversationParticipantsAddedDomainEventV2>(),
                It.IsAny<AggregateDeltaBatch<ConversationParticipant>>(),
                cancellationToken))
            .Callback<ConversationParticipantsAddedDomainEventV2, AggregateDeltaBatch<ConversationParticipant>, CancellationToken>(
                (_, batch, _) => capturedBatch = batch)
            .Returns(Task.CompletedTask);

        var conversationId = Id<ConversationV2>.New();
        var participantUserIds = CreateUserIds(3);
        var notification = new ConversationParticipantsAddedDomainEventV2(
            Id<ConversationMembership>.FromId(conversationId),
            conversationId,
            participantUserIds,
            initialReadCursor: 42);
        var handler = new ConversationParticipantsAddedDomainEventHandlerV2(
            repository.Object,
            dispatcher.Object,
            [processor.Object],
            [deltaProcessor.Object]);

        await handler.Handle(notification, cancellationToken);

        addedParticipants.Select(participant => participant.UserId).Should().Equal(participantUserIds);
        addedParticipants.Should().OnlyContain(participant =>
            participant.ConversationId == conversationId &&
            participant.LastReadMessageSequenceNum == 42);
        processedParticipants.Should().Equal(addedParticipants);
        processedParticipants.Should().OnlyContain(participant =>
            participant.Version == 2 &&
            participant.CreatedBy == "system" &&
            participant.LastModifiedBy == "system" &&
            participant.CreatedAtUtc != null &&
            participant.LastModifiedAtUtc != null &&
            !participant.IsDeleted);
        capturedBatch.Should().NotBeNull();
        capturedBatch.DeltaProjectionMetadata.Should().Be(
            new DeltaProjectionMetadataV2(
                conversationId.Value,
                notification.Version));
        capturedBatch.Mutations.Select(mutation => mutation.Aggregate).Should().Equal(addedParticipants);
        capturedBatch.Mutations.Should().OnlyContain(mutation => mutation.MutationType == MutationType.Created);
        deltaProcessor.Verify(instance => instance.ProcessAsync(
            notification,
            It.IsAny<AggregateDeltaBatch<ConversationParticipant>>(),
            cancellationToken), Times.Once);
    }

    private static IReadOnlyList<Id<UserProfileMarker>> CreateUserIds(int count) =>
        Enumerable.Range(0, count)
            .Select(_ => Id<UserProfileMarker>.New())
            .ToArray();
}
