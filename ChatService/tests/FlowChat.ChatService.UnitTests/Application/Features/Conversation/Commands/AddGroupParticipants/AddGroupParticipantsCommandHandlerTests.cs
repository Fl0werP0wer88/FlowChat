using FlowChat.ChatService.Application.Contracts.Persistence;
using FlowChat.ChatService.Application.Features.Conversation.Commands.AddGroupParticipants;
using FlowChat.ChatService.Domain.Entities.Conversation;
using FlowChat.Core.Messaging;
using FlowChat.Core.Results;
using FlowChat.Shared.Application;
using FlowChat.Shared.Application.CommandHandlers.AggregateRootCommandHandlerBaseV2.BeforeSaveProcessors;
using FlowChat.Shared.Domain;
using FluentAssertions;
using Moq;
using ConversationAggregate = FlowChat.ChatService.Domain.Entities.Conversation.Conversation;


namespace FlowChat.ChatService.UnitTests.Application.Features.Conversation.Commands.AddGroupParticipants;

public sealed class AddGroupParticipantsCommandHandlerTests
{
    private readonly Mock<IGroupConversationWriteRepository> _conversationRepositoryMock = new();
    private readonly Mock<IChatMessageWriteRepository> _chatMessageRepositoryMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<ILocalEventDispatcher> _domainEventDispatcherMock = new();
    private readonly Mock<IAggregateBeforeSaveProcessor<AddGroupParticipantsCommand, GroupConversation>> _beforeSaveProcessorMock = new();
    private readonly AddGroupParticipantsCommandHandler _handler;

    public AddGroupParticipantsCommandHandlerTests()
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
                It.IsAny<AddGroupParticipantsCommand>(),
                It.IsAny<GroupConversation>(),
                It.IsAny<MutationType>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _handler = new AddGroupParticipantsCommandHandler(
            _conversationRepositoryMock.Object,
            _chatMessageRepositoryMock.Object,
            _unitOfWorkMock.Object,
            _domainEventDispatcherMock.Object,
            [_beforeSaveProcessorMock.Object]);
    }

    [Fact]
    public async Task Handle_ValidRequest_AddsParticipant()
    {
        var creatorId = Guid.NewGuid();
        var existingMemberId = Guid.NewGuid();
        var newMemberId = Guid.NewGuid();
        var conversationId = Guid.NewGuid();
        var command = new AddGroupParticipantsCommand(conversationId, [newMemberId]);

        var conversation = GroupConversation.Create(
            Id<ConversationAggregate>.FromGuid(conversationId),
            creatorId,
            [creatorId, existingMemberId],
            "Dev Team");
        conversation.ClearEvents();

        _conversationRepositoryMock
            .Setup(x => x.GetByIdAsync(conversationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(conversation);
        _chatMessageRepositoryMock
            .Setup(x => x.GetMaxSequenceNumAsync(conversationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(42);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeTrue();
        conversation.Participants.Should().ContainSingle(p =>
            p.UserId.Value == newMemberId &&
            p.LastReadMessageSequenceNum == 42);
    }

    [Fact]
    public async Task Handle_MultipleNewParticipants_AddsAllWithSameLastReadMessageSequenceNum()
    {
        var creatorId = Guid.NewGuid();
        var newMemberId1 = Guid.NewGuid();
        var newMemberId2 = Guid.NewGuid();
        var conversationId = Guid.NewGuid();
        var command = new AddGroupParticipantsCommand(conversationId, [newMemberId1, newMemberId2]);

        var conversation = GroupConversation.Create(
            Id<ConversationAggregate>.FromGuid(conversationId),
            creatorId,
            [creatorId, Guid.NewGuid()],
            "Dev Team");
        conversation.ClearEvents();

        _conversationRepositoryMock
            .Setup(x => x.GetByIdAsync(conversationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(conversation);
        _chatMessageRepositoryMock
            .Setup(x => x.GetMaxSequenceNumAsync(conversationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(84);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeTrue();
        conversation.Participants.Should().ContainSingle(p =>
            p.UserId.Value == newMemberId1 &&
            p.LastReadMessageSequenceNum == 84);
        conversation.Participants.Should().ContainSingle(p =>
            p.UserId.Value == newMemberId2 &&
            p.LastReadMessageSequenceNum == 84);
        _chatMessageRepositoryMock.Verify(
            x => x.GetMaxSequenceNumAsync(conversationId, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_DuplicateNewParticipants_AddsParticipantOnce()
    {
        var creatorId = Guid.NewGuid();
        var newMemberId = Guid.NewGuid();
        var conversationId = Guid.NewGuid();
        var command = new AddGroupParticipantsCommand(conversationId, [newMemberId, newMemberId]);

        var conversation = GroupConversation.Create(
            Id<ConversationAggregate>.FromGuid(conversationId),
            creatorId,
            [creatorId, Guid.NewGuid()],
            "Dev Team");
        conversation.ClearEvents();

        _conversationRepositoryMock
            .Setup(x => x.GetByIdAsync(conversationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(conversation);
        _chatMessageRepositoryMock
            .Setup(x => x.GetMaxSequenceNumAsync(conversationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(7);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeTrue();
        conversation.Participants.Should().ContainSingle(p =>
            p.UserId.Value == newMemberId &&
            p.LastReadMessageSequenceNum == 7);
        _chatMessageRepositoryMock.Verify(
            x => x.GetMaxSequenceNumAsync(conversationId, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_MixedParticipants_AddsOnlyNewOnesAndReturnsTrue()
    {
        var creatorId = Guid.NewGuid();
        var existingMemberId = Guid.NewGuid();
        var newMemberId = Guid.NewGuid();
        var conversationId = Guid.NewGuid();
        var command = new AddGroupParticipantsCommand(conversationId, [existingMemberId, newMemberId]);

        var conversation = GroupConversation.Create(
            Id<ConversationAggregate>.FromGuid(conversationId),
            creatorId,
            [creatorId, existingMemberId],
            "Dev Team");
        conversation.ClearEvents();

        _conversationRepositoryMock
            .Setup(x => x.GetByIdAsync(conversationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(conversation);
        _chatMessageRepositoryMock
            .Setup(x => x.GetMaxSequenceNumAsync(conversationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(21);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeTrue();
        conversation.Participants.Should().ContainSingle(p =>
            p.UserId.Value == newMemberId &&
            p.LastReadMessageSequenceNum == 21);
    }

    [Fact]
    public async Task Handle_WhenMaxSequenceNumIsMissing_AddsParticipantWithZeroLastReadMessageSequenceNum()
    {
        var creatorId = Guid.NewGuid();
        var existingMemberId = Guid.NewGuid();
        var newMemberId = Guid.NewGuid();
        var conversationId = Guid.NewGuid();
        var command = new AddGroupParticipantsCommand(conversationId, [newMemberId]);

        var conversation = GroupConversation.Create(
            Id<ConversationAggregate>.FromGuid(conversationId),
            creatorId,
            [creatorId, existingMemberId],
            "Dev Team");
        conversation.ClearEvents();

        _conversationRepositoryMock
            .Setup(x => x.GetByIdAsync(conversationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(conversation);
        _chatMessageRepositoryMock
            .Setup(x => x.GetMaxSequenceNumAsync(conversationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((long?)null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeTrue();
        conversation.Participants.Should().ContainSingle(p =>
            p.UserId.Value == newMemberId &&
            p.LastReadMessageSequenceNum == 0);
    }

    [Fact]
    public async Task Handle_WhenAllParticipantsAlreadyInConversation_ReturnsSuccessWithoutDispatchingEvents()
    {
        var creatorId = Guid.NewGuid();
        var existingMemberId = Guid.NewGuid();
        var conversationId = Guid.NewGuid();
        var command = new AddGroupParticipantsCommand(conversationId, [existingMemberId]);

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
                It.IsAny<AddGroupParticipantsCommand>(),
                It.IsAny<GroupConversation>(),
                It.IsAny<MutationType>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
        _chatMessageRepositoryMock.Verify(
            x => x.GetMaxSequenceNumAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WhenGroupConversationNotFound_ReturnsNotFound()
    {
        var conversationId = Guid.NewGuid();
        var command = new AddGroupParticipantsCommand(conversationId, [Guid.NewGuid()]);

        _conversationRepositoryMock
            .Setup(x => x.GetByIdAsync(conversationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((GroupConversation?)null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.ErrorType.Should().Be(ErrorType.NotFound);
    }
}

