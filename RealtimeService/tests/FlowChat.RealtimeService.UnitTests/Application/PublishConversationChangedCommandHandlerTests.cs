using AutoFixture;
using FlowChat.RealtimeService.Application.Contracts.Infrastructure;
using FlowChat.RealtimeService.Application.Features.Conversation.Commands.PublishConversationChanged;
using FluentAssertions;
using Moq;

namespace FlowChat.RealtimeService.UnitTests;

public sealed class PublishConversationChangedCommandHandlerTests
{
    private readonly IFixture _fixture = new Fixture();
    private readonly Mock<IRealtimeClientDispatcher> _dispatcherMock = new();
    private readonly PublishConversationChangedCommandHandler _handler;

    public PublishConversationChangedCommandHandlerTests()
    {
        _dispatcherMock
            .Setup(x => x.ConversationChangedAsync(It.IsAny<ConversationChangedParam>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _handler = new PublishConversationChangedCommandHandler(_dispatcherMock.Object);
    }

    [Fact]
    public async Task Handle_NormalizesParticipantsAndDispatches()
    {
        ConversationChangedParam? capturedNotification = null;
        var participantUserId = _fixture.Create<Guid>();

        _dispatcherMock
            .Setup(x => x.ConversationChangedAsync(It.IsAny<ConversationChangedParam>(), It.IsAny<CancellationToken>()))
            .Callback<ConversationChangedParam, CancellationToken>((notification, _) => capturedNotification = notification)
            .Returns(Task.CompletedTask);

        var result = await _handler.Handle(
            new PublishConversationChangedCommand(
                _fixture.Create<Guid>(),
                2,
                " Dev Team ",
                _fixture.Create<Guid>(),
                [participantUserId, participantUserId, Guid.Empty]),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        capturedNotification.Should().NotBeNull();
        capturedNotification!.Name.Should().Be(" Dev Team ");
        capturedNotification.ParticipantUserIds.Should().ContainSingle().Which.Should().Be(participantUserId);
    }
}
