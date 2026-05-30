using FlowChat.ChatService.Application.Contracts.Persistence;
using FlowChat.ChatService.Application.Features.Conversation.Commands.CreateGroupConversation;
using FlowChat.ChatService.Application.Features.Conversation.Dtos;
using FlowChat.ChatService.Application.Features.UserProfile;
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
    private readonly Mock<IGroupConversationWriteRepository> _writeRepositoryMock = new();
    private readonly Mock<IGroupConversationReadRepository> _readRepositoryMock = new();
    private readonly Mock<IUserProfileProjectionReadRepository> _profileReadRepositoryMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<ILocalEventDispatcher> _domainEventDispatcherMock = new();
    private readonly Mock<IDbUpdateExceptionClassifier> _dbUpdateExceptionClassifierMock = new();
    private readonly CreateGroupConversationCommandHandler _handler;

    public CreateGroupConversationCommandHandlerTests()
    {
        _unitOfWorkMock
            .Setup(x => x.ExecuteCommandInTransactionAsync(
                It.IsAny<Func<CancellationToken, Task<FlowChatResult<IdempotentCommandResult<GroupConversationDetailDto>>>>>(),
                It.IsAny<CancellationToken>()))
            .Returns<Func<CancellationToken, Task<FlowChatResult<IdempotentCommandResult<GroupConversationDetailDto>>>>, CancellationToken>(
                (operation, ct) => operation(ct));

        _domainEventDispatcherMock
            .Setup(x => x.DispatchAsync(It.IsAny<IEnumerable<IDomainEvent>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _handler = new CreateGroupConversationCommandHandler(
            _writeRepositoryMock.Object,
            _readRepositoryMock.Object,
            _profileReadRepositoryMock.Object,
            _unitOfWorkMock.Object,
            _domainEventDispatcherMock.Object,
            _dbUpdateExceptionClassifierMock.Object);
    }

    [Fact]
    public async Task Handle_ValidRequest_ReturnsSuccessWithDetailDtoAndDispatchesEvents()
    {
        var conversationId = Guid.NewGuid();
        var creatorId = Guid.NewGuid();
        var memberId = Guid.NewGuid();
        var command = new CreateGroupConversationCommand(conversationId, creatorId, [creatorId, memberId], "Dev Team");

        GroupConversation? persisted = null;
        List<IDomainEvent> dispatchedEvents = [];

        _writeRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<GroupConversation>(), It.IsAny<CancellationToken>()))
            .Callback<GroupConversation, CancellationToken>((c, _) => persisted = c)
            .ReturnsAsync((GroupConversation c, CancellationToken _) => c);
        _profileReadRepositoryMock
            .Setup(x => x.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(
            [
                new UserProfileConversationParticipantDto(creatorId, "Creator", "creator.png"),
                new UserProfileConversationParticipantDto(memberId, "Member", "member.png")
            ]);
        _domainEventDispatcherMock
            .Setup(x => x.DispatchAsync(It.IsAny<IEnumerable<IDomainEvent>>(), It.IsAny<CancellationToken>()))
            .Callback<IEnumerable<IDomainEvent>, CancellationToken>((events, _) => dispatchedEvents.AddRange(events))
            .Returns(Task.CompletedTask);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.WasAlreadyProcessed.Should().BeFalse();
        result.Value.Value.ConversationId.Should().Be(conversationId);
        result.Value.Value.Name.Should().Be("Dev Team");
        result.Value.Value.Participants.Should().HaveCount(2);
        result.Value.Value.Participants.Select(p => p.UserId).Should().BeEquivalentTo([creatorId, memberId]);
        persisted.Should().NotBeNull();
        persisted!.Id.Value.Should().Be(conversationId);
        dispatchedEvents.OfType<ConversationCreatedDomainEvent>().Should().ContainSingle();
    }

    [Fact]
    public async Task Handle_WhenConversationIdAlreadyExists_ReturnsExistingDtoWithoutDispatchingEvents()
    {
        var conversationId = Guid.NewGuid();
        var creatorId = Guid.NewGuid();
        var memberId = Guid.NewGuid();
        var command = new CreateGroupConversationCommand(conversationId, creatorId, [creatorId, memberId], "Dev Team");

        var existingDto = new GroupConversationDetailDto(
            conversationId,
            "Dev Team",
            [
                new ConversationParticipantDto(creatorId, "Creator", "creator.png", creatorId),
                new ConversationParticipantDto(memberId, "Member", "member.png", memberId)
            ]);
        List<IDomainEvent> dispatchedEvents = [];

        _writeRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<GroupConversation>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new DbUpdateException("duplicate"));
        _dbUpdateExceptionClassifierMock
            .Setup(x => x.IsIdempotencyConflict(It.IsAny<DbUpdateException>(), CreateGroupConversationCommand.IdempotencyConflictKey))
            .Returns(true);
        _readRepositoryMock
            .Setup(x => x.GetByIdAsync(conversationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingDto);
        _domainEventDispatcherMock
            .Setup(x => x.DispatchAsync(It.IsAny<IEnumerable<IDomainEvent>>(), It.IsAny<CancellationToken>()))
            .Callback<IEnumerable<IDomainEvent>, CancellationToken>((events, _) => dispatchedEvents.AddRange(events))
            .Returns(Task.CompletedTask);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.WasAlreadyProcessed.Should().BeTrue();
        result.Value.Value.Should().Be(existingDto);
        dispatchedEvents.Should().BeEmpty();
        _profileReadRepositoryMock.Verify(
            x => x.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}

