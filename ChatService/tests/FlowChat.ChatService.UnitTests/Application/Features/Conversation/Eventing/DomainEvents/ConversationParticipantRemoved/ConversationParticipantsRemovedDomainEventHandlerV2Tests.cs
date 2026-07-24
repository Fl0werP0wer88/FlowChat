using FlowChat.ChatService.Application.Contracts.Persistence;
using FlowChat.ChatService.Application.Features.Conversation.Eventing.DomainEvents.ConversationParticipantRemoved;
using FlowChat.ChatService.Domain.Entities.Conversation;
using FlowChat.ChatService.Domain.Entities.Conversation.Events;
using FlowChat.Core.Exceptions;
using FlowChat.Core.Messaging;
using FlowChat.Shared.Application;
using FlowChat.Shared.Application.CommandHandlers.AggregateRootCommandHandlerBaseV2.BeforeSaveProcessors;
using FlowChat.Shared.Application.CommandHandlers.BatchAggregateCommandHandlerBase.BeforeSaveProcessors;
using FlowChat.Shared.Domain;
using FluentAssertions;
using Moq;
using UserProfileMarker = FlowChat.ChatService.Domain.Entities.UserProfiles.UserProfile;

namespace FlowChat.ChatService.UnitTests.Application.Features.Conversation.Eventing.DomainEvents.ConversationParticipantRemoved;

public sealed class ConversationParticipantsRemovedDomainEventHandlerV2Tests
{
    [Fact]
    public async Task Handle_WhenRepositoryReturnsDifferentOrder_ProcessesParticipantsInEventOrder()
    {
        var repository = new Mock<IConversationParticipantWriteRepository>();
        var dispatcher = new Mock<ILocalEventDispatcher>();
        var processor = new Mock<IAggregateBeforeSaveProcessorV2<
            ConversationParticipantsRemovedDomainEventV2,
            ConversationParticipant>>();
        var deltaProcessor = new Mock<IAggregateBeforeSaveDeltaProcessorV2<
            ConversationParticipantsRemovedDomainEventV2,
            ConversationParticipant>>();
        var processedParticipants = new List<ConversationParticipant>();
        AggregateDeltaBatch<ConversationParticipant>? capturedBatch = null;
        var cancellationToken = new CancellationTokenSource().Token;
        var conversationId = Id<ConversationV2>.New();
        var participantUserIds = CreateUserIds(3);
        var participants = participantUserIds
            .Select(userId => ConversationParticipant.Create(
                Id<ConversationParticipant>.New(),
                conversationId,
                ConversationType.Group,
                userId))
            .ToArray();

        repository.Setup(instance => instance.GetActiveByUserIdsAsync(
                conversationId,
                participantUserIds,
                cancellationToken))
            .ReturnsAsync(participants.Reverse().ToArray());
        processor.Setup(instance => instance.ProcessAsync(
                It.IsAny<ConversationParticipantsRemovedDomainEventV2>(),
                It.IsAny<ConversationParticipant>(),
                MutationType.Deleted,
                cancellationToken))
            .Callback<ConversationParticipantsRemovedDomainEventV2, ConversationParticipant, MutationType, CancellationToken>(
                (_, participant, _, _) => processedParticipants.Add(participant))
            .Returns(Task.CompletedTask);
        deltaProcessor.Setup(instance => instance.ProcessAsync(
                It.IsAny<ConversationParticipantsRemovedDomainEventV2>(),
                It.IsAny<AggregateDeltaBatch<ConversationParticipant>>(),
                cancellationToken))
            .Callback<ConversationParticipantsRemovedDomainEventV2, AggregateDeltaBatch<ConversationParticipant>, CancellationToken>(
                (_, batch, _) => capturedBatch = batch)
            .Returns(Task.CompletedTask);

        var notification = new ConversationParticipantsRemovedDomainEventV2(
            Id<ConversationMembership>.FromId(conversationId),
            conversationId,
            participantUserIds);
        var handler = new ConversationParticipantsRemovedDomainEventHandlerV2(
            repository.Object,
            dispatcher.Object,
            [processor.Object],
            [deltaProcessor.Object]);

        await handler.Handle(notification, cancellationToken);

        processedParticipants.Should().Equal(participants);
        processedParticipants.Should().OnlyContain(participant =>
            participant.Version == 2 &&
            participant.CreatedBy == string.Empty &&
            participant.LastModifiedBy == "system" &&
            participant.LastModifiedAtUtc != null &&
            participant.IsDeleted &&
            participant.DeletedAt != null);
        capturedBatch.Should().NotBeNull();
        capturedBatch.DeltaProjectionMetadata.Should().Be(
            new DeltaProjectionMetadataV2(
                conversationId.Value,
                notification.Version));
        capturedBatch.Mutations.Select(mutation => mutation.Aggregate).Should().Equal(participants);
        capturedBatch.Mutations.Should().OnlyContain(mutation => mutation.MutationType == MutationType.Deleted);
        deltaProcessor.Verify(instance => instance.ProcessAsync(
            notification,
            It.IsAny<AggregateDeltaBatch<ConversationParticipant>>(),
            cancellationToken), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenAnyParticipantIsMissing_FailsBeforeMutatingFetchedParticipants()
    {
        var repository = new Mock<IConversationParticipantWriteRepository>();
        var dispatcher = new Mock<ILocalEventDispatcher>();
        var processor = new Mock<IAggregateBeforeSaveProcessorV2<
            ConversationParticipantsRemovedDomainEventV2,
            ConversationParticipant>>();
        var deltaProcessor = new Mock<IAggregateBeforeSaveDeltaProcessorV2<
            ConversationParticipantsRemovedDomainEventV2,
            ConversationParticipant>>();
        var conversationId = Id<ConversationV2>.New();
        var participantUserIds = CreateUserIds(2);
        var fetchedParticipant = ConversationParticipant.Create(
            Id<ConversationParticipant>.New(),
            conversationId,
            ConversationType.Group,
            participantUserIds[0]);
        var notification = new ConversationParticipantsRemovedDomainEventV2(
            Id<ConversationMembership>.FromId(conversationId),
            conversationId,
            participantUserIds);
        repository.Setup(instance => instance.GetActiveByUserIdsAsync(
                conversationId,
                participantUserIds,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([fetchedParticipant]);
        var handler = new ConversationParticipantsRemovedDomainEventHandlerV2(
            repository.Object,
            dispatcher.Object,
            [processor.Object],
            [deltaProcessor.Object]);

        var action = () => handler.Handle(notification, CancellationToken.None);

        await action.Should().ThrowAsync<ResultException>();
        fetchedParticipant.Version.Should().Be(1);
        fetchedParticipant.LastModifiedBy.Should().BeEmpty();
        fetchedParticipant.IsDeleted.Should().BeFalse();
        dispatcher.Verify(instance => instance.DispatchAsync(
            It.IsAny<IEnumerable<ILocalEvent>>(),
            It.IsAny<CancellationToken>()), Times.Never);
        processor.Verify(instance => instance.ProcessAsync(
            It.IsAny<ConversationParticipantsRemovedDomainEventV2>(),
            It.IsAny<ConversationParticipant>(),
            It.IsAny<MutationType>(),
            It.IsAny<CancellationToken>()), Times.Never);
        deltaProcessor.Verify(instance => instance.ProcessAsync(
            It.IsAny<ConversationParticipantsRemovedDomainEventV2>(),
            It.IsAny<AggregateDeltaBatch<ConversationParticipant>>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    private static IReadOnlyList<Id<UserProfileMarker>> CreateUserIds(int count) =>
        Enumerable.Range(0, count)
            .Select(_ => Id<UserProfileMarker>.New())
            .ToArray();
}
