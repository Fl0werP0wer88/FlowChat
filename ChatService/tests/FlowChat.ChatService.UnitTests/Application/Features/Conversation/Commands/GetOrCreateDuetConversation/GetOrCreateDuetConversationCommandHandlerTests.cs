using FlowChat.ChatService.Application.Contracts.Persistence;
using FlowChat.ChatService.Application.Features.Conversation.Commands.GetOrCreateDuetConversation;
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

public sealed class GetOrCreateDuetConversationCommandHandlerTests
{
    private readonly Mock<IConversationWriteRepository> _conversationWriteRepositoryMock = new();
    private readonly Mock<IDuetConversationReadRepository> _duetConversationReadRepositoryMock = new();
    private readonly Mock<IDuetConversationRepository> _duetConversationRepositoryMock = new();
    private readonly Mock<IUserProfileProjectionReadRepository> _userProfileProjectionReadRepositoryMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<IDomainEventDispatcher> _domainEventDispatcherMock = new();
    private readonly GetOrCreateDuetConversationCommandHandler _handler;

    public GetOrCreateDuetConversationCommandHandlerTests()
    {
        _unitOfWorkMock
            .Setup(x => x.ExecuteInTransactionAsync(
                It.IsAny<Func<CancellationToken, Task<FlowChatResult<DuetConversationDetailDto>>>>(),
                It.IsAny<CancellationToken>()))
            .Returns<Func<CancellationToken, Task<FlowChatResult<DuetConversationDetailDto>>>, CancellationToken>(
                (operation, ct) => operation(ct));

        _domainEventDispatcherMock
            .Setup(x => x.DispatchAsync(It.IsAny<IEnumerable<IDomainEvent>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _handler = new GetOrCreateDuetConversationCommandHandler(
            _conversationWriteRepositoryMock.Object,
            _duetConversationReadRepositoryMock.Object,
            _duetConversationRepositoryMock.Object,
            _userProfileProjectionReadRepositoryMock.Object,
            _unitOfWorkMock.Object,
            _domainEventDispatcherMock.Object);
    }

    [Fact]
    public async Task Handle_WhenDuetConversationExists_ReturnsProjectedConversation()
    {
        var command = new GetOrCreateDuetConversationCommand(Guid.NewGuid(), Guid.NewGuid());
        var dto = new DuetConversationDetailDto(
            Guid.NewGuid(),
            [
                new ConversationParticipantDto(command.RequestingUserId, "Requester", "requester.png", "requester"),
                new ConversationParticipantDto(command.PartnerUserId, "Partner", "partner.png", "partner")
            ]);

        _duetConversationReadRepositoryMock
            .Setup(x => x.GetByUserIdsAsync(command.RequestingUserId, command.PartnerUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(dto);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(dto);
        _duetConversationRepositoryMock.Verify(
            x => x.FindConversationIdAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _conversationWriteRepositoryMock.Verify(
            x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _conversationWriteRepositoryMock.Verify(
            x => x.AddAsync(It.IsAny<ConversationAggregate>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _userProfileProjectionReadRepositoryMock.Verify(
            x => x.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WhenReadModelIsMissingButDuetEntryExists_ReturnsNotFound()
    {
        var command = new GetOrCreateDuetConversationCommand(Guid.NewGuid(), Guid.NewGuid());

        _duetConversationReadRepositoryMock
            .Setup(x => x.GetByUserIdsAsync(command.RequestingUserId, command.PartnerUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((DuetConversationDetailDto?)null);
        _duetConversationRepositoryMock
            .Setup(x => x.FindConversationIdAsync(command.RequestingUserId, command.PartnerUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Guid.NewGuid());

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.ErrorType.Should().Be(ErrorType.NotFound);
        result.Error.ErrorMessage.Should().Be("Conversation not found.");
        _conversationWriteRepositoryMock.Verify(
            x => x.AddAsync(It.IsAny<ConversationAggregate>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _userProfileProjectionReadRepositoryMock.Verify(
            x => x.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WhenDuetConversationDoesNotExist_CreatesConversationAndDispatchesDomainEvents()
    {
        var command = new GetOrCreateDuetConversationCommand(Guid.NewGuid(), Guid.NewGuid());
        ConversationAggregate? persistedConversation = null;
        List<IDomainEvent> dispatchedEvents = [];

        _duetConversationReadRepositoryMock
            .Setup(x => x.GetByUserIdsAsync(command.RequestingUserId, command.PartnerUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((DuetConversationDetailDto?)null);
        _duetConversationRepositoryMock
            .Setup(x => x.FindConversationIdAsync(command.RequestingUserId, command.PartnerUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid?)null);
        _conversationWriteRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<ConversationAggregate>(), It.IsAny<CancellationToken>()))
            .Callback<ConversationAggregate, CancellationToken>((conversation, _) => persistedConversation = conversation)
            .ReturnsAsync((ConversationAggregate conversation, CancellationToken _) => conversation);
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
        persistedConversation.Should().NotBeNull();
        result.Value.ConversationId.Should().Be(persistedConversation!.Id.Value);
        result.Value.Participants.Select(x => x.UserId).Should().Equal(command.RequestingUserId, command.PartnerUserId);
        result.Value.Participants.Select(x => x.FriendlyUserId).Should().Equal("requester", "partner");
        dispatchedEvents.Should().ContainSingle(x => x is ConversationCreatedDomainEvent);
        dispatchedEvents.Should().ContainSingle(
            x => x is AggregateStateChangedDomainEvent<ConversationAggregate, ConversationSnapshot>);
        _duetConversationRepositoryMock.Verify(
            x => x.AddAsync(command.RequestingUserId, command.PartnerUserId, persistedConversation.Id.Value, It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
