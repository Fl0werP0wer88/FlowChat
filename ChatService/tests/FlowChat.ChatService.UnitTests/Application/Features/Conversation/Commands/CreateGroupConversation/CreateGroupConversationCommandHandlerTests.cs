using FlowChat.ChatService.Application.Contracts.Persistence;
using FlowChat.ChatService.Application.Features.Conversation.Commands.CreateGroupConversation;
using FlowChat.ChatService.Application.Features.Conversation.Dtos;
using FlowChat.ChatService.Application.Features.UserProfile;
using FlowChat.ChatService.Domain.Entities.Conversation.Events;
using FlowChat.Core.Messaging;
using FlowChat.Core.Results;
using FlowChat.Shared.Application;
using FlowChat.Shared.Domain;
using FluentAssertions;
using Moq;
using GroupConversation = FlowChat.ChatService.Domain.Entities.Conversation.GroupConversation;

namespace FlowChat.ChatService.UnitTests.Application.Features.Conversation.Commands.CreateGroupConversation;

public sealed class CreateGroupConversationCommandHandlerTests
{
    private readonly Mock<IGroupConversationWriteRepository> _writeRepositoryMock = new();
    private readonly Mock<IUserProfileProjectionReadRepository> _profileReadRepositoryMock = new();
    private readonly Mock<IConversationMessageSequenceRepository> _sequenceRepositoryMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<ILocalEventDispatcher> _domainEventDispatcherMock = new();
    private readonly CreateGroupConversationCommandHandler _handler;

    public CreateGroupConversationCommandHandlerTests()
    {
        _unitOfWorkMock
            .Setup(x => x.ExecuteCommandInTransactionAsync(
                It.IsAny<Func<CancellationToken, Task<FlowChatResult<GroupConversationDetailDto>>>>(),
                It.IsAny<CancellationToken>()))
            .Returns<Func<CancellationToken, Task<FlowChatResult<GroupConversationDetailDto>>>, CancellationToken>(
                (operation, ct) => operation(ct));

        _domainEventDispatcherMock
            .Setup(x => x.DispatchAsync(It.IsAny<IEnumerable<ILocalEvent>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _handler = new CreateGroupConversationCommandHandler(
            _writeRepositoryMock.Object,
            _profileReadRepositoryMock.Object,
            _sequenceRepositoryMock.Object,
            _unitOfWorkMock.Object,
            _domainEventDispatcherMock.Object,
            []);
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
            .Setup(x => x.DispatchAsync(It.IsAny<IEnumerable<ILocalEvent>>(), It.IsAny<CancellationToken>()))
            .Callback<IEnumerable<ILocalEvent>, CancellationToken>((events, _) => dispatchedEvents.AddRange(events.OfType<IDomainEvent>()))
            .Returns(Task.CompletedTask);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.ConversationId.Should().Be(conversationId);
        result.Value.Name.Should().Be("Dev Team");
        result.Value.Participants.Should().HaveCount(2);
        result.Value.Participants.Select(p => p.UserId).Should().BeEquivalentTo([creatorId, memberId]);
        persisted.Should().NotBeNull();
        persisted!.Id.Value.Should().Be(conversationId);
        _sequenceRepositoryMock.Verify(x => x.AddAsync(conversationId, It.IsAny<CancellationToken>()), Times.Once);
        dispatchedEvents.OfType<GroupConversationCreatedDomainEvent>().Should().ContainSingle();
    }
}

