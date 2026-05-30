using FlowChat.ChatService.Application.Contracts.Persistence;
using FlowChat.ChatService.Application.Features.Conversation.Commands.CreateDuetConversation;
using FlowChat.ChatService.Application.Features.Conversation.Dtos;
using FlowChat.ChatService.Application.Features.UserProfile;
using FlowChat.ChatService.Domain.Entities.Conversation;
using FlowChat.ChatService.Domain.Entities.Conversation.Events;
using FlowChat.Core.Results;
using FlowChat.Shared.Application;
using FlowChat.Shared.Domain;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using ConversationAggregate = FlowChat.ChatService.Domain.Entities.Conversation.Conversation;

namespace FlowChat.ChatService.UnitTests.Application.Features.Conversation.Commands.CreateDuetConversation;

public sealed class CreateDuetConversationCommandHandlerTests
{
    private readonly Mock<IDuetConversationReadRepository> _duetConversationReadRepositoryMock = new();
    private readonly Mock<IDuetConversationWriteRepository> _duetConversationWriteRepositoryMock = new();
    private readonly Mock<IUserProfileProjectionReadRepository> _userProfileProjectionReadRepositoryMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<ILocalEventDispatcher> _domainEventDispatcherMock = new();
    private readonly Mock<IDbUpdateExceptionClassifier> _dbUpdateExceptionClassifierMock = new();
    private readonly CreateDuetConversationCommandHandler _handler;

    public CreateDuetConversationCommandHandlerTests()
    {
        _unitOfWorkMock
            .Setup(x => x.ExecuteCommandInTransactionAsync(
                It.IsAny<Func<CancellationToken, Task<FlowChatResult<IdempotentCommandResult<DuetConversationDetailDto>>>>>(),
                It.IsAny<CancellationToken>()))
            .Returns<Func<CancellationToken, Task<FlowChatResult<IdempotentCommandResult<DuetConversationDetailDto>>>>, CancellationToken>(
                (operation, ct) => operation(ct));

        _domainEventDispatcherMock
            .Setup(x => x.DispatchAsync(It.IsAny<IEnumerable<IDomainEvent>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _handler = new CreateDuetConversationCommandHandler(
            _duetConversationReadRepositoryMock.Object,
            _duetConversationWriteRepositoryMock.Object,
            _userProfileProjectionReadRepositoryMock.Object,
            _unitOfWorkMock.Object,
            _domainEventDispatcherMock.Object,
            _dbUpdateExceptionClassifierMock.Object);
    }

    [Fact]
    public async Task Handle_WhenDuetConversationDoesNotExist_CreatesConversationAndDispatchesDomainEvents()
    {
        var command = new CreateDuetConversationCommand(Guid.NewGuid(), Guid.NewGuid());
        ConversationAggregate? persistedConversation = null;
        List<IDomainEvent> dispatchedEvents = [];

        _duetConversationWriteRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<DuetConversation>(), It.IsAny<CancellationToken>()))
            .Callback<DuetConversation, CancellationToken>((c, _) => persistedConversation = c)
            .ReturnsAsync((DuetConversation c, CancellationToken _) => c);
        _userProfileProjectionReadRepositoryMock
            .Setup(x => x.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(
            [
                new UserProfileConversationParticipantDto(command.PartnerUserId, "Partner", "partner.png"),
                new UserProfileConversationParticipantDto(command.RequestingUserId, "Requester", "requester.png")
            ]);
        _domainEventDispatcherMock
            .Setup(x => x.DispatchAsync(It.IsAny<IEnumerable<IDomainEvent>>(), It.IsAny<CancellationToken>()))
            .Callback<IEnumerable<IDomainEvent>, CancellationToken>((events, _) => dispatchedEvents.AddRange(events))
            .Returns(Task.CompletedTask);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.WasAlreadyProcessed.Should().BeFalse();
        persistedConversation.Should().NotBeNull();
        result.Value.Value.ConversationId.Should().Be(persistedConversation!.Id.Value);
        result.Value.Value.Participants.Select(x => x.UserId).Should().Equal(command.RequestingUserId, command.PartnerUserId);
        result.Value.Value.Participants.Select(x => x.ParticipantUserId).Should().Equal(command.RequestingUserId, command.PartnerUserId);
        dispatchedEvents.Should().ContainSingle(x => x is ConversationCreatedDomainEvent);
        dispatchedEvents.Should().ContainSingle(
            x => x is AggregateStateChangedDomainEvent<ConversationAggregate, ConversationSnapshot>);
        _duetConversationWriteRepositoryMock.Verify(
            x => x.AddAsync(It.IsAny<DuetConversation>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WhenConversationAlreadyExistsViaDuplicateKeyException_ReturnsExistingConversationWithoutDispatchingEvents()
    {
        var command = new CreateDuetConversationCommand(Guid.NewGuid(), Guid.NewGuid());
        var existingDto = new DuetConversationDetailDto(
            Guid.NewGuid(),
            [
                new ConversationParticipantDto(command.RequestingUserId, "Requester", "requester.png", command.RequestingUserId),
                new ConversationParticipantDto(command.PartnerUserId, "Partner", "partner.png", command.PartnerUserId)
            ]);
        List<IDomainEvent> dispatchedEvents = [];

        _duetConversationWriteRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<DuetConversation>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new DbUpdateException("duplicate"));
        _dbUpdateExceptionClassifierMock
            .Setup(x => x.IsIdempotencyConflict(It.IsAny<DbUpdateException>(), CreateDuetConversationCommand.IdempotencyConflictKey))
            .Returns(true);
        _duetConversationReadRepositoryMock
            .Setup(x => x.GetByUserIdsAsync(command.RequestingUserId, command.PartnerUserId, It.IsAny<CancellationToken>()))
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
        _userProfileProjectionReadRepositoryMock.Verify(
            x => x.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}

