using FlowChat.ChatService.Application.Contracts.Persistence;
using FlowChat.ChatService.Application.Features.Conversation.Commands.CreateGroupFromDuet;
using FlowChat.ChatService.Application.Features.Conversation.Dtos;
using FlowChat.ChatService.Application.Features.UserProfile;
using FlowChat.Core.Results;
using FlowChat.Shared.Application;
using FlowChat.Shared.Domain;
using FluentAssertions;
using Moq;
using GroupConversation = FlowChat.ChatService.Domain.Entities.Conversation.GroupConversation;

namespace FlowChat.ChatService.UnitTests.Application.Features.Conversation.Commands.CreateGroupFromDuet;

public sealed class CreateGroupFromDuetCommandHandlerTests
{
    private readonly Mock<IDuetConversationReadRepository> _duetReadRepositoryMock = new();
    private readonly Mock<IGroupConversationWriteRepository> _groupWriteRepositoryMock = new();
    private readonly Mock<IUserProfileProjectionReadRepository> _profileReadRepositoryMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<IDomainEventDispatcher> _domainEventDispatcherMock = new();
    private readonly CreateGroupFromDuetCommandHandler _handler;

    public CreateGroupFromDuetCommandHandlerTests()
    {
        _unitOfWorkMock
            .Setup(x => x.ExecuteInTransactionAsync(
                It.IsAny<Func<CancellationToken, Task<FlowChatResult<GroupConversationDetailDto>>>>(),
                It.IsAny<CancellationToken>()))
            .Returns<Func<CancellationToken, Task<FlowChatResult<GroupConversationDetailDto>>>, CancellationToken>(
                (operation, ct) => operation(ct));

        _domainEventDispatcherMock
            .Setup(x => x.DispatchAsync(It.IsAny<IEnumerable<IDomainEvent>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _groupWriteRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<GroupConversation>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((GroupConversation c, CancellationToken _) => c);

        _profileReadRepositoryMock
            .Setup(x => x.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        _handler = new CreateGroupFromDuetCommandHandler(
            _duetReadRepositoryMock.Object,
            _groupWriteRepositoryMock.Object,
            _profileReadRepositoryMock.Object,
            _unitOfWorkMock.Object,
            _domainEventDispatcherMock.Object);
    }

    [Fact]
    public async Task Handle_WhenDuetExists_CreatesGroupWithCorrectName()
    {
        var requestingUserId = Guid.NewGuid();
        var partnerUserId = Guid.NewGuid();
        var command = new CreateGroupFromDuetCommand(requestingUserId, partnerUserId);

        _duetReadRepositoryMock
            .Setup(x => x.GetByUserIdsAsync(requestingUserId, partnerUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DuetConversationDetailDto(Guid.NewGuid(),
            [
                new ConversationParticipantDto(requestingUserId, "Alice", null, requestingUserId),
                new ConversationParticipantDto(partnerUserId, "Bob", null, partnerUserId)
            ]));

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be("Alice/Bob");
    }

    [Fact]
    public async Task Handle_WhenDuetExists_CreatesGroupWithBothParticipants()
    {
        var requestingUserId = Guid.NewGuid();
        var partnerUserId = Guid.NewGuid();
        var command = new CreateGroupFromDuetCommand(requestingUserId, partnerUserId);

        _duetReadRepositoryMock
            .Setup(x => x.GetByUserIdsAsync(requestingUserId, partnerUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DuetConversationDetailDto(Guid.NewGuid(),
            [
                new ConversationParticipantDto(requestingUserId, "Alice", null, requestingUserId),
                new ConversationParticipantDto(partnerUserId, "Bob", null, partnerUserId)
            ]));

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Participants.Select(p => p.UserId)
            .Should().BeEquivalentTo([requestingUserId, partnerUserId]);
    }

    [Fact]
    public async Task Handle_WhenDuetNotFound_ReturnsFailureWithNotFound()
    {
        var command = new CreateGroupFromDuetCommand(Guid.NewGuid(), Guid.NewGuid());

        _duetReadRepositoryMock
            .Setup(x => x.GetByUserIdsAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((DuetConversationDetailDto?)null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.ErrorType.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task Handle_WhenDisplayNameIsNull_FallsBackToUserId()
    {
        var requestingUserId = Guid.NewGuid();
        var partnerUserId = Guid.NewGuid();
        var command = new CreateGroupFromDuetCommand(requestingUserId, partnerUserId);

        _duetReadRepositoryMock
            .Setup(x => x.GetByUserIdsAsync(requestingUserId, partnerUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DuetConversationDetailDto(Guid.NewGuid(),
            [
                new ConversationParticipantDto(requestingUserId, null, null, requestingUserId),
                new ConversationParticipantDto(partnerUserId, null, null, partnerUserId)
            ]));

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be($"{requestingUserId:D}/{partnerUserId:D}");
    }

    [Fact]
    public async Task Handle_WhenDuetExists_SavesGroupConversation()
    {
        var requestingUserId = Guid.NewGuid();
        var partnerUserId = Guid.NewGuid();
        var command = new CreateGroupFromDuetCommand(requestingUserId, partnerUserId);

        _duetReadRepositoryMock
            .Setup(x => x.GetByUserIdsAsync(requestingUserId, partnerUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DuetConversationDetailDto(Guid.NewGuid(),
            [
                new ConversationParticipantDto(requestingUserId, "Alice", null, requestingUserId),
                new ConversationParticipantDto(partnerUserId, "Bob", null, partnerUserId)
            ]));

        await _handler.Handle(command, CancellationToken.None);

        _groupWriteRepositoryMock.Verify(
            x => x.AddAsync(It.IsAny<GroupConversation>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
