using FlowChat.ChatService.Application.Contracts.Persistence;
using FlowChat.ChatService.Application.Features.Conversation.Commands.CreateDuetConversation;
using FlowChat.ChatService.Application.Features.Conversation.Dtos;
using FlowChat.ChatService.Domain.Entities.Conversation;
using FlowChat.ChatService.Domain.Entities.Conversation.Events;
using FlowChat.Core.Results;
using FlowChat.Shared.Application;
using FlowChat.Shared.Domain;
using FluentAssertions;
using Moq;
using ConversationAggregate = FlowChat.ChatService.Domain.Entities.Conversation.Conversation;

namespace FlowChat.ChatService.UnitTests;

public sealed class CreateDuetConversationCommandHandlerTests
{
    private readonly Mock<IDuetConversationReadRepository> _duetConversationReadRepositoryMock = new();
    private readonly Mock<IDuetConversationWriteRepository> _duetConversationWriteRepositoryMock = new();
    private readonly Mock<IUserProfileProjectionReadRepository> _userProfileProjectionReadRepositoryMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<IDomainEventDispatcher> _domainEventDispatcherMock = new();
    private readonly CreateDuetConversationCommandHandler _handler;

    public CreateDuetConversationCommandHandlerTests()
    {
        _unitOfWorkMock
            .Setup(x => x.ExecuteInTransactionAsync(
                It.IsAny<Func<CancellationToken, Task<FlowChatResult<CreateDuetConversationResult>>>>(),
                It.IsAny<CancellationToken>()))
            .Returns<Func<CancellationToken, Task<FlowChatResult<CreateDuetConversationResult>>>, CancellationToken>(
                (operation, ct) => operation(ct));

        _domainEventDispatcherMock
            .Setup(x => x.DispatchAsync(It.IsAny<IEnumerable<IDomainEvent>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _handler = new CreateDuetConversationCommandHandler(
            _duetConversationReadRepositoryMock.Object,
            _duetConversationWriteRepositoryMock.Object,
            _userProfileProjectionReadRepositoryMock.Object,
            _unitOfWorkMock.Object,
            _domainEventDispatcherMock.Object);
    }

    [Fact]
    public async Task Handle_WhenDuetConversationAlreadyExists_ReturnsExistingConversation()
    {
        var command = new CreateDuetConversationCommand(Guid.NewGuid(), Guid.NewGuid());
        var existingConversation = new DuetConversationDetailDto(
            Guid.NewGuid(),
            [
                new ConversationParticipantDto(command.RequestingUserId, "Requester", "requester.png", "requester"),
                new ConversationParticipantDto(command.PartnerUserId, "Partner", "partner.png", "partner")
            ]);
        List<IDomainEvent> dispatchedEvents = [];

        _duetConversationReadRepositoryMock
            .Setup(x => x.GetByUserIdsAsync(command.RequestingUserId, command.PartnerUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingConversation);
        _domainEventDispatcherMock
            .Setup(x => x.DispatchAsync(It.IsAny<IEnumerable<IDomainEvent>>(), It.IsAny<CancellationToken>()))
            .Callback<IEnumerable<IDomainEvent>, CancellationToken>((events, _) => dispatchedEvents.AddRange(events))
            .Returns(Task.CompletedTask);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.WasCreated.Should().BeFalse();
        result.Value.Conversation.Should().Be(existingConversation);
        _duetConversationWriteRepositoryMock.Verify(
            x => x.AddAsync(
                It.IsAny<DuetConversation>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
        _userProfileProjectionReadRepositoryMock.Verify(
            x => x.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()),
            Times.Never);
        dispatchedEvents.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_WhenDuetConversationDoesNotExist_CreatesConversationAndDispatchesDomainEvents()
    {
        var command = new CreateDuetConversationCommand(Guid.NewGuid(), Guid.NewGuid());
        ConversationAggregate? persistedConversation = null;
        List<IDomainEvent> dispatchedEvents = [];

        _duetConversationReadRepositoryMock
            .Setup(x => x.GetByUserIdsAsync(command.RequestingUserId, command.PartnerUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((DuetConversationDetailDto?)null);
        _duetConversationWriteRepositoryMock
            .Setup(x => x.AddAsync(
                It.IsAny<DuetConversation>(),
                It.IsAny<CancellationToken>()))
            .Callback<DuetConversation, CancellationToken>((conversation, _) => persistedConversation = conversation)
            .ReturnsAsync((DuetConversation conversation, CancellationToken _) => conversation);
        _userProfileProjectionReadRepositoryMock
            .Setup(x => x.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(
            [
                new ConversationParticipantDto(command.PartnerUserId, "Partner", "partner.png", "partner"),
                new ConversationParticipantDto(command.RequestingUserId, "Requester", "requester.png", "requester")
            ]);
        _domainEventDispatcherMock
            .Setup(x => x.DispatchAsync(It.IsAny<IEnumerable<IDomainEvent>>(), It.IsAny<CancellationToken>()))
            .Callback<IEnumerable<IDomainEvent>, CancellationToken>((events, _) => dispatchedEvents.AddRange(events))
            .Returns(Task.CompletedTask);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.WasCreated.Should().BeTrue();
        persistedConversation.Should().NotBeNull();
        persistedConversation!.Id.Value.Should().NotBeEmpty();
        result.Value.Conversation.ConversationId.Should().Be(persistedConversation!.Id.Value);
        result.Value.Conversation.Participants.Select(x => x.UserId).Should().Equal(command.RequestingUserId, command.PartnerUserId);
        result.Value.Conversation.Participants.Select(x => x.FriendlyUserId).Should().Equal("requester", "partner");
        dispatchedEvents.Should().ContainSingle(x => x is ConversationCreatedDomainEvent);
        dispatchedEvents.Should().ContainSingle(
            x => x is AggregateStateChangedDomainEvent<ConversationAggregate, ConversationSnapshot>);
        _duetConversationWriteRepositoryMock.Verify(
            x => x.AddAsync(
                It.IsAny<DuetConversation>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
