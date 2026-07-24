using FlowChat.ChatService.Application.Contracts.Persistence;
using FlowChat.ChatService.Application.Features.Conversation.Commands.RemoveGroupParticipants;
using FlowChat.ChatService.Domain.Entities.Conversation;
using FlowChat.ChatService.Domain.Entities.Conversation.Events;
using FlowChat.ChatService.Domain.Entities.UserProfiles;
using FlowChat.Core.Messaging;
using FlowChat.Core.Results;
using FlowChat.Shared.Application;
using FlowChat.Shared.Application.CommandHandlers.AggregateRootCommandHandlerBaseV2.BeforeSaveProcessors;
using FlowChat.Shared.Domain;
using FluentAssertions;
using MediatR;
using Moq;

namespace FlowChat.ChatService.UnitTests.Application.Features.Conversation.Commands.RemoveGroupParticipants;

public sealed class RemoveGroupParticipantsCommandHandlerV2Tests
{
    [Fact]
    public async Task Handle_MixedParticipants_RemovesOnlyActiveParticipantsInRequestOrder()
    {
        var conversationId = Id<ConversationV2>.New();
        var existingUserId1 = Id<UserProfile>.New();
        var existingUserId2 = Id<UserProfile>.New();
        var missingUserId = Id<UserProfile>.New();
        var membership = CreateMembership(
            conversationId,
            existingUserId1,
            existingUserId2,
            Id<UserProfile>.New(),
            Id<UserProfile>.New());
        var membershipRepository = CreateMembershipRepository(conversationId, membership);
        var participantRepository = new Mock<IConversationParticipantWriteRepository>();
        participantRepository.Setup(x => x.GetActiveByUserIdsAsync(
                conversationId,
                It.IsAny<IReadOnlyCollection<Id<UserProfile>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                ConversationParticipant.Create(
                    Id<ConversationParticipant>.New(),
                    conversationId,
                    ConversationType.Group,
                    existingUserId1,
                    duetPartnerUserId: null),
                ConversationParticipant.Create(
                    Id<ConversationParticipant>.New(),
                    conversationId,
                    ConversationType.Group,
                    existingUserId2,
                    duetPartnerUserId: null)
            ]);
        var dispatchedEvents = new List<ILocalEvent>();
        var dispatcher = CreateDispatcher(dispatchedEvents);
        var processor = CreateProcessor();
        var command = new RemoveGroupParticipantsCommandV2(
            conversationId.Value,
            [existingUserId2.Value, missingUserId.Value, existingUserId1.Value]);
        var handler = new RemoveGroupParticipantsCommandHandlerV2(
            membershipRepository.Object,
            participantRepository.Object,
            CreateUnitOfWork().Object,
            dispatcher.Object,
            [processor.Object]);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        membership.ParticipantCount.Should().Be(2);
        membership.Version.Should().Be(2);
        var domainEvent = dispatchedEvents.Should()
            .ContainSingle()
            .Which.Should().BeOfType<ConversationParticipantsRemovedDomainEventV2>().Subject;
        domainEvent.ParticipantUserIds.Should().Equal(existingUserId2, existingUserId1);
        processor.Verify(x => x.ProcessAsync(
            command,
            membership,
            MutationType.Updated,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenNoRequestedParticipantIsActive_ReturnsUnchangedWithoutSideEffects()
    {
        var conversationId = Id<ConversationV2>.New();
        var membership = CreateMembership(
            conversationId,
            Id<UserProfile>.New(),
            Id<UserProfile>.New());
        var membershipRepository = CreateMembershipRepository(conversationId, membership);
        var participantRepository = new Mock<IConversationParticipantWriteRepository>();
        participantRepository.Setup(x => x.GetActiveByUserIdsAsync(
                conversationId,
                It.IsAny<IReadOnlyCollection<Id<UserProfile>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        var dispatcher = new Mock<ILocalEventDispatcher>();
        var processor = CreateProcessor();
        var handler = new RemoveGroupParticipantsCommandHandlerV2(
            membershipRepository.Object,
            participantRepository.Object,
            CreateUnitOfWork().Object,
            dispatcher.Object,
            [processor.Object]);

        var result = await handler.Handle(
            new RemoveGroupParticipantsCommandV2(conversationId.Value, [Guid.NewGuid()]),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        membership.Version.Should().Be(1);
        dispatcher.Verify(x => x.DispatchAsync(
            It.IsAny<IEnumerable<ILocalEvent>>(),
            It.IsAny<CancellationToken>()), Times.Never);
        processor.Verify(x => x.ProcessAsync(
            It.IsAny<RemoveGroupParticipantsCommandV2>(),
            It.IsAny<ConversationMembership>(),
            It.IsAny<MutationType>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenEffectiveRemovalWouldLeaveFewerThanTwoParticipants_ReturnsBadRequest()
    {
        var conversationId = Id<ConversationV2>.New();
        var existingUserId = Id<UserProfile>.New();
        var membership = CreateMembership(conversationId, existingUserId, Id<UserProfile>.New());
        var membershipRepository = CreateMembershipRepository(conversationId, membership);
        var participantRepository = new Mock<IConversationParticipantWriteRepository>();
        participantRepository.Setup(x => x.GetActiveByUserIdsAsync(
                conversationId,
                It.IsAny<IReadOnlyCollection<Id<UserProfile>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                ConversationParticipant.Create(
                    Id<ConversationParticipant>.New(),
                    conversationId,
                    ConversationType.Group,
                    existingUserId,
                    duetPartnerUserId: null)
            ]);
        var handler = new RemoveGroupParticipantsCommandHandlerV2(
            membershipRepository.Object,
            participantRepository.Object,
            CreateUnitOfWork().Object,
            Mock.Of<ILocalEventDispatcher>(),
            []);

        var result = await handler.Handle(
            new RemoveGroupParticipantsCommandV2(
                conversationId.Value,
                [existingUserId.Value, Guid.NewGuid()]),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.ErrorType.Should().Be(ErrorType.BadRequest);
        membership.ParticipantCount.Should().Be(2);
        membership.Version.Should().Be(1);
    }

    [Fact]
    public async Task Handle_WhenConversationIsNotAGroup_ReturnsBadRequestBeforeParticipantLookup()
    {
        var conversationId = Id<ConversationV2>.New();
        var membership = ConversationMembership.Create(
            conversationId,
            ConversationType.Duet,
            [Id<UserProfile>.New(), Id<UserProfile>.New()]);
        membership.ClearEvents();
        var membershipRepository = CreateMembershipRepository(conversationId, membership);
        var participantRepository = new Mock<IConversationParticipantWriteRepository>();
        var handler = new RemoveGroupParticipantsCommandHandlerV2(
            membershipRepository.Object,
            participantRepository.Object,
            CreateUnitOfWork().Object,
            Mock.Of<ILocalEventDispatcher>(),
            []);

        var result = await handler.Handle(
            new RemoveGroupParticipantsCommandV2(conversationId.Value, [Guid.NewGuid()]),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.ErrorType.Should().Be(ErrorType.BadRequest);
        participantRepository.Verify(x => x.GetActiveByUserIdsAsync(
            It.IsAny<Id<ConversationV2>>(),
            It.IsAny<IReadOnlyCollection<Id<UserProfile>>>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    private static ConversationMembership CreateMembership(
        Id<ConversationV2> conversationId,
        params Id<UserProfile>[] userIds)
    {
        var membership = ConversationMembership.Create(conversationId, ConversationType.Group, userIds);
        membership.ClearEvents();
        return membership;
    }

    private static Mock<IConversationMembershipWriteRepository> CreateMembershipRepository(
        Id<ConversationV2> conversationId,
        ConversationMembership membership)
    {
        var repository = new Mock<IConversationMembershipWriteRepository>();
        repository.Setup(x => x.GetByConversationIdAsync(conversationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(membership);
        return repository;
    }

    private static Mock<ILocalEventDispatcher> CreateDispatcher(ICollection<ILocalEvent> dispatchedEvents)
    {
        var dispatcher = new Mock<ILocalEventDispatcher>();
        dispatcher.Setup(x => x.DispatchAsync(
                It.IsAny<IEnumerable<ILocalEvent>>(),
                It.IsAny<CancellationToken>()))
            .Callback<IEnumerable<ILocalEvent>, CancellationToken>((events, _) =>
            {
                foreach (var domainEvent in events)
                {
                    dispatchedEvents.Add(domainEvent);
                }
            })
            .Returns(Task.CompletedTask);
        return dispatcher;
    }

    private static Mock<IAggregateBeforeSaveProcessorV2<
        RemoveGroupParticipantsCommandV2,
        ConversationMembership>> CreateProcessor()
    {
        var processor = new Mock<IAggregateBeforeSaveProcessorV2<
            RemoveGroupParticipantsCommandV2,
            ConversationMembership>>();
        processor.Setup(x => x.ProcessAsync(
                It.IsAny<RemoveGroupParticipantsCommandV2>(),
                It.IsAny<ConversationMembership>(),
                It.IsAny<MutationType>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        return processor;
    }

    private static Mock<IUnitOfWork> CreateUnitOfWork()
    {
        var unitOfWork = new Mock<IUnitOfWork>();
        unitOfWork.Setup(x => x.ExecuteCommandInTransactionAsync(
                It.IsAny<Func<CancellationToken, Task<FlowChatResult<Unit>>>>(),
                It.IsAny<CancellationToken>()))
            .Returns<Func<CancellationToken, Task<FlowChatResult<Unit>>>, CancellationToken>(
                (operation, cancellationToken) => operation(cancellationToken));
        return unitOfWork;
    }
}
