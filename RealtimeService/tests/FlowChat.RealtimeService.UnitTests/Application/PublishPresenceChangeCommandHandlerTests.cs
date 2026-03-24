using FlowChat.Domain.Abstractions;
using FlowChat.RealtimeService.Application.Features.Presence.Commands.PublishPresenceChange;

namespace FlowChat.RealtimeService.UnitTests;

public sealed class PublishPresenceChangeCommandHandlerTests
{
    [Fact]
    public async Task Handle_NormalizesStatusAndDispatches()
    {
        var dispatcher = new CapturingRealtimeClientDispatcher();
        var handler = new PublishPresenceChangeCommandHandler(dispatcher);
        var recipientUserId = Guid.NewGuid();

        var result = await handler.Handle(
            new PublishPresenceChangeCommand(
                Guid.NewGuid(),
                " Online ",
                new DateTime(2026, 3, 17, 12, 30, 0, DateTimeKind.Utc),
                [recipientUserId]),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(dispatcher.LastPresenceNotification);
        Assert.Equal("online", dispatcher.LastPresenceNotification!.Status);
        Assert.Equal(recipientUserId, dispatcher.LastPresenceNotification.RecipientUserIds.Single());
    }

    [Fact]
    public async Task Handle_WhenStatusIsInvalid_ReturnsBadRequestFailure()
    {
        var dispatcher = new CapturingRealtimeClientDispatcher();
        var handler = new PublishPresenceChangeCommandHandler(dispatcher);

        var result = await handler.Handle(
            new PublishPresenceChangeCommand(Guid.NewGuid(), "busy", DateTime.UtcNow, [Guid.NewGuid()]),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.BadRequest, result.Error.ErrorType);
        Assert.Null(dispatcher.LastPresenceNotification);
    }
}
