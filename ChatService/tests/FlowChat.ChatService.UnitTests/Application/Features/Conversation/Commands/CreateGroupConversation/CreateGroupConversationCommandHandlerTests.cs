using FlowChat.ChatService.Application.Contracts.Persistence;
using FlowChat.ChatService.Application.Features.Conversation.Commands.CreateGroupConversation;
using FlowChat.ChatService.Domain.Entities.Conversation.Events;
using FlowChat.Core.Results;
using FlowChat.Shared.Application;
using FlowChat.Shared.Domain;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using GroupConversation = FlowChat.ChatService.Domain.Entities.Conversation.GroupConversation;

namespace FlowChat.ChatService.UnitTests.Application.Features.Conversation.Commands.CreateGroupConversation;

public sealed class CreateGroupConversationCommandHandlerTests
{
    private readonly Mock<IGroupConversationWriteRepository> _repositoryMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<IDomainEventDispatcher> _domainEventDispatcherMock = new();
    private readonly Mock<IDbUpdateExceptionClassifier> _dbUpdateExceptionClassifierMock = new();
    private readonly CreateGroupConversationCommandHandler _handler;

    public CreateGroupConversationCommandHandlerTests()
    {
        _unitOfWorkMock
            .Setup(x => x.ExecuteInTransactionAsync(
                It.IsAny<Func<CancellationToken, Task<FlowChatResult<IdempotentCommandResult<Guid>>>>>(),
                It.IsAny<CancellationToken>()))
            .Returns<Func<CancellationToken, Task<FlowChatResult<IdempotentCommandResult<Guid>>>>, CancellationToken>(
                (operation, ct) => operation(ct));

        _domainEventDispatcherMock
            .Setup(x => x.DispatchAsync(It.IsAny<IEnumerable<IDomainEvent>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _handler = new CreateGroupConversationCommandHandler(
            _repositoryMock.Object,
            _unitOfWorkMock.Object,
            _domainEventDispatcherMock.Object,
            _dbUpdateExceptionClassifierMock.Object);
    }

    [Fact]
    public async Task Handle_ValidRequest_ReturnsSuccessAndDispatchesConversationCreatedEvent()
    {
        var conversationId = Guid.NewGuid();
        var creatorId = Guid.NewGuid();
        var memberId = Guid.NewGuid();
        var command = new CreateGroupConversationCommand(conversationId, creatorId, [creatorId, memberId], "Dev Team");

        GroupConversation? persisted = null;
        List<IDomainEvent> dispatchedEvents = [];

        _repositoryMock
            .Setup(x => x.AddAsync(It.IsAny<GroupConversation>(), It.IsAny<CancellationToken>()))
            .Callback<GroupConversation, CancellationToken>((c, _) => persisted = c)
            .ReturnsAsync((GroupConversation c, CancellationToken _) => c);

        _domainEventDispatcherMock
            .Setup(x => x.DispatchAsync(It.IsAny<IEnumerable<IDomainEvent>>(), It.IsAny<CancellationToken>()))
            .Callback<IEnumerable<IDomainEvent>, CancellationToken>((events, _) => dispatchedEvents.AddRange(events))
            .Returns(Task.CompletedTask);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.WasAlreadyProcessed.Should().BeFalse();
        result.Value.Value.Should().Be(conversationId);
        persisted.Should().NotBeNull();
        persisted!.Id.Value.Should().Be(conversationId);
        persisted.Name.Should().Be("Dev Team");
        persisted.CreatedByUserId.Should().Be(creatorId);
        persisted.Participants.Select(p => p.UserId).Should().BeEquivalentTo([creatorId, memberId]);
        dispatchedEvents.OfType<ConversationCreatedDomainEvent>().Should().ContainSingle();
    }

    [Fact]
    public async Task Handle_WhenConversationIdAlreadyExists_ReturnsExistingResponseWithoutDispatchingEvents()
    {
        var conversationId = Guid.NewGuid();
        var creatorId = Guid.NewGuid();
        var memberId = Guid.NewGuid();
        var command = new CreateGroupConversationCommand(conversationId, creatorId, [creatorId, memberId], "Dev Team");

        List<IDomainEvent> dispatchedEvents = [];

        _repositoryMock
            .Setup(x => x.AddAsync(It.IsAny<GroupConversation>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new DbUpdateException("duplicate"));
        _dbUpdateExceptionClassifierMock
            .Setup(x => x.IsExpectedIdempotencyConflict(It.IsAny<DbUpdateException>(), CreateGroupConversationCommand.IdempotencyConflictKey))
            .Returns(true);

        _domainEventDispatcherMock
            .Setup(x => x.DispatchAsync(It.IsAny<IEnumerable<IDomainEvent>>(), It.IsAny<CancellationToken>()))
            .Callback<IEnumerable<IDomainEvent>, CancellationToken>((events, _) => dispatchedEvents.AddRange(events))
            .Returns(Task.CompletedTask);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.WasAlreadyProcessed.Should().BeTrue();
        result.Value.Value.Should().Be(conversationId);
        dispatchedEvents.Should().BeEmpty();
    }
}
