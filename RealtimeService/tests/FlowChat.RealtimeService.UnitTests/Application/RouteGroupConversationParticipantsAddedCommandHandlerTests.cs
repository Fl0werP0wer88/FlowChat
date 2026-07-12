using AutoFixture;
using FlowChat.RealtimeService.Application.Contracts.Infrastructure;
using FlowChat.RealtimeService.Application.Contracts.Persistence;
using FlowChat.RealtimeService.Application.Features.Conversation.Commands.RouteGroupConversationParticipantsAdded;
using FlowChat.RealtimeService.Domain.Enums;
using FluentAssertions;
using Moq;

namespace FlowChat.RealtimeService.UnitTests;

public sealed class RouteGroupConversationParticipantsAddedCommandHandlerTests
{
    private readonly IFixture _fixture = new Fixture();
    private readonly Mock<IRealtimeGroupMembershipReadModelRepository> _readModelRepositoryMock = new();
    private readonly Mock<IRealtimeGroupMembershipVersionTrackerRepository> _versionTrackerRepositoryMock = new();
    private readonly Mock<IRealtimeEventRouter> _routerMock = new();
    private readonly RouteGroupConversationParticipantsAddedCommandHandler _handler;

    public RouteGroupConversationParticipantsAddedCommandHandlerTests()
    {
        _routerMock
            .Setup(x => x.RouteGroupConversationParticipantsAddedAsync(It.IsAny<GroupConversationParticipantsAddedParam>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _handler = new RouteGroupConversationParticipantsAddedCommandHandler(
            _readModelRepositoryMock.Object,
            _versionTrackerRepositoryMock.Object,
            _routerMock.Object);
    }

    [Fact]
    public async Task Handle_NormalizesParticipants_PersistsReadModelAndRoutes()
    {
        var conversationId = _fixture.Create<Guid>();
        var participantUserId = _fixture.Create<Guid>();
        var conversationVersion = _fixture.Create<int>();
        GroupConversationParticipantsAddedParam? capturedNotification = null;

        _routerMock
            .Setup(x => x.RouteGroupConversationParticipantsAddedAsync(It.IsAny<GroupConversationParticipantsAddedParam>(), It.IsAny<CancellationToken>()))
            .Callback<GroupConversationParticipantsAddedParam, CancellationToken>((notification, _) => capturedNotification = notification)
            .Returns(Task.CompletedTask);

        var result = await _handler.Handle(
            new RouteGroupConversationParticipantsAddedCommand(conversationId, [participantUserId, participantUserId, Guid.Empty], conversationVersion),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        _readModelRepositoryMock.Verify(
            x => x.AddRangeAsync(
                It.Is<IReadOnlyCollection<Guid>>(ids => ids.SequenceEqual(new[] { participantUserId })),
                RealtimeGroupType.Conversation,
                conversationId,
                It.IsAny<CancellationToken>()),
            Times.Once);
        _versionTrackerRepositoryMock.Verify(
            x => x.UpsertIfNewerAsync(conversationId, conversationVersion, It.IsAny<CancellationToken>()),
            Times.Once);
        capturedNotification.Should().NotBeNull();
        capturedNotification!.ParticipantUserIds.Should().ContainSingle().Which.Should().Be(participantUserId);
    }
}
