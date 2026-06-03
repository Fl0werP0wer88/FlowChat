using FlowChat.ChatService.Application.Contracts.Persistence;
using FlowChat.ChatService.Application.Features.Conversation.Commands.AddParticipant;
using FlowChat.ChatService.Domain.Entities.Conversation;
using FlowChat.ChatService.Domain.Entities.Conversation.Events;
using FlowChat.Core.Messaging;
using FlowChat.Core.Results;
using FlowChat.Shared.Application;
using FlowChat.Shared.Application.CommandHandlers.AggregateRootCommandHandlerBaseV2.BeforeSaveProcessors;
using FlowChat.Shared.Domain;
using FluentAssertions;
using Moq;
using ConversationAggregate = FlowChat.ChatService.Domain.Entities.Conversation.Conversation;


namespace FlowChat.ChatService.UnitTests.Application.Features.Conversation.Commands.AddParticipant;

public sealed class AddParticipantCommandHandlerTests
{
    private readonly Mock<IGroupConversationWriteRepository> _conversationRepositoryMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<ILocalEventDispatcher> _domainEventDispatcherMock = new();
    private readonly Mock<IAggregateBeforeSaveProcessor<AddParticipantCommand, GroupConversation>> _beforeSaveProcessorMock = new();
    private readonly AddParticipantCommandHandler _handler;

    public AddParticipantCommandHandlerTests()
    {
        _unitOfWorkMock
            .Setup(x => x.ExecuteCommandInTransactionAsync(
                It.IsAny<Func<CancellationToken, Task<FlowChatResult<bool>>>>(),
                It.IsAny<CancellationToken>()))
            .Returns<Func<CancellationToken, Task<FlowChatResult<bool>>>, CancellationToken>(
                (operation, ct) => operation(ct));

        _domainEventDispatcherMock
            .Setup(x => x.DispatchAsync(It.IsAny<IEnumerable<ILocalEvent>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _beforeSaveProcessorMock
            .Setup(x => x.ProcessAsync(
                It.IsAny<AddParticipantCommand>(),
                It.IsAny<GroupConversation>(),
                It.IsAny<AggregateState>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _handler = new AddParticipantCommandHandler(
            _conversationRepositoryMock.Object,
            _unitOfWorkMock.Object,
            _domainEventDispatcherMock.Object,
            [_beforeSaveProcessorMock.Object]);
    }

    [Fact]
    public async Task Handle_ValidRequest_AddsParticipantAndDispatchesEvents()
    {
        var creatorId = Guid.NewGuid();
        var existingMemberId = Guid.NewGuid();
        var newMemberId = Guid.NewGuid();
        var conversationId = Guid.NewGuid();
        var command = new AddParticipantCommand(conversationId, [newMemberId]);

        var conversation = GroupConversation.Create(
            Id<ConversationAggregate>.FromGuid(conversationId),
            creatorId,
            [creatorId, existingMemberId],
            "Dev Team");
        conversation.ClearEvents();

        List<IDomainEvent> dispatchedEvents = [];

        _conversationRepositoryMock
            .Setup(x => x.GetByIdAsync(conversationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(conversation);
        _domainEventDispatcherMock
            .Setup(x => x.DispatchAsync(It.IsAny<IEnumerable<ILocalEvent>>(), It.IsAny<CancellationToken>()))
            .Callback<IEnumerable<ILocalEvent>, CancellationToken>((events, _) => dispatchedEvents.AddRange(events.OfType<IDomainEvent>()))
            .Returns(Task.CompletedTask);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeTrue();
        conversation.Participants.Should().Contain(p => p.UserId.Value == newMemberId);
        dispatchedEvents.OfType<ParticipantAddedDomainEvent>().Should().ContainSingle()
            .Which.ParticipantUserId.Value.Should().Be(newMemberId);
    }

    [Fact]
    public async Task Handle_MultipleNewParticipants_AddsAllAndDispatchesEventsForEach()
    {
        var creatorId = Guid.NewGuid();
        var newMemberId1 = Guid.NewGuid();
        var newMemberId2 = Guid.NewGuid();
        var conversationId = Guid.NewGuid();
        var command = new AddParticipantCommand(conversationId, [newMemberId1, newMemberId2]);

        var conversation = GroupConversation.Create(
            Id<ConversationAggregate>.FromGuid(conversationId),
            creatorId,
            [creatorId, Guid.NewGuid()],
            "Dev Team");
        conversation.ClearEvents();

        List<IDomainEvent> dispatchedEvents = [];

        _conversationRepositoryMock
            .Setup(x => x.GetByIdAsync(conversationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(conversation);
        _domainEventDispatcherMock
            .Setup(x => x.DispatchAsync(It.IsAny<IEnumerable<ILocalEvent>>(), It.IsAny<CancellationToken>()))
            .Callback<IEnumerable<ILocalEvent>, CancellationToken>((events, _) => dispatchedEvents.AddRange(events.OfType<IDomainEvent>()))
            .Returns(Task.CompletedTask);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeTrue();
        conversation.Participants.Should().Contain(p => p.UserId.Value == newMemberId1);
        conversation.Participants.Should().Contain(p => p.UserId.Value == newMemberId2);
        dispatchedEvents.OfType<ParticipantAddedDomainEvent>().Should().HaveCount(2);
    }

    [Fact]
    public async Task Handle_MixedParticipants_AddsOnlyNewOnesAndReturnsTrue()
    {
        var creatorId = Guid.NewGuid();
        var existingMemberId = Guid.NewGuid();
        var newMemberId = Guid.NewGuid();
        var conversationId = Guid.NewGuid();
        var command = new AddParticipantCommand(conversationId, [existingMemberId, newMemberId]);

        var conversation = GroupConversation.Create(
            Id<ConversationAggregate>.FromGuid(conversationId),
            creatorId,
            [creatorId, existingMemberId],
            "Dev Team");
        conversation.ClearEvents();

        List<IDomainEvent> dispatchedEvents = [];

        _conversationRepositoryMock
            .Setup(x => x.GetByIdAsync(conversationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(conversation);
        _domainEventDispatcherMock
            .Setup(x => x.DispatchAsync(It.IsAny<IEnumerable<ILocalEvent>>(), It.IsAny<CancellationToken>()))
            .Callback<IEnumerable<ILocalEvent>, CancellationToken>((events, _) => dispatchedEvents.AddRange(events.OfType<IDomainEvent>()))
            .Returns(Task.CompletedTask);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeTrue();
        dispatchedEvents.OfType<ParticipantAddedDomainEvent>().Should().ContainSingle()
            .Which.ParticipantUserId.Value.Should().Be(newMemberId);
    }

    [Fact]
    public async Task Handle_WhenAllParticipantsAlreadyInConversation_ReturnsSuccessWithoutDispatchingEvents()
    {
        var creatorId = Guid.NewGuid();
        var existingMemberId = Guid.NewGuid();
        var conversationId = Guid.NewGuid();
        var command = new AddParticipantCommand(conversationId, [existingMemberId]);

        var conversation = GroupConversation.Create(
            Id<ConversationAggregate>.FromGuid(conversationId),
            creatorId,
            [creatorId, existingMemberId],
            "Dev Team");
        conversation.ClearEvents();

        List<IDomainEvent> dispatchedEvents = [];

        _conversationRepositoryMock
            .Setup(x => x.GetByIdAsync(conversationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(conversation);
        _domainEventDispatcherMock
            .Setup(x => x.DispatchAsync(It.IsAny<IEnumerable<ILocalEvent>>(), It.IsAny<CancellationToken>()))
            .Callback<IEnumerable<ILocalEvent>, CancellationToken>((events, _) => dispatchedEvents.AddRange(events.OfType<IDomainEvent>()))
            .Returns(Task.CompletedTask);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeFalse();
        dispatchedEvents.Should().BeEmpty();
        _domainEventDispatcherMock.Verify(
            x => x.DispatchAsync(It.IsAny<IEnumerable<ILocalEvent>>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _beforeSaveProcessorMock.Verify(
            x => x.ProcessAsync(
                It.IsAny<AddParticipantCommand>(),
                It.IsAny<GroupConversation>(),
                It.IsAny<AggregateState>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WhenGroupConversationNotFound_ReturnsNotFound()
    {
        var conversationId = Guid.NewGuid();
        var command = new AddParticipantCommand(conversationId, [Guid.NewGuid()]);

        _conversationRepositoryMock
            .Setup(x => x.GetByIdAsync(conversationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((GroupConversation?)null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.ErrorType.Should().Be(ErrorType.NotFound);
    }
}

