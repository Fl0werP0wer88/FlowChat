using FlowChat.RealtimeService.Application.Presence.Commands.PublishPresenceChange;

namespace FlowChat.RealtimeService.UnitTests;

public sealed class PublishPresenceChangeCommandHandlerTests
{
    [Fact]
    public async Task Handle_NormalizesStatusAndDispatches()
    {
        var dispatcher = new CapturingRealtimeClientDispatcher();
        var handler = new PublishPresenceChangeCommandHandler(dispatcher);
        var recipientUserId = Guid.NewGuid();

        await handler.Handle(
            new PublishPresenceChangeCommand(
                Guid.NewGuid(),
                " Online ",
                new DateTime(2026, 3, 17, 12, 30, 0, DateTimeKind.Utc),
                [recipientUserId]),
            CancellationToken.None);

        Assert.NotNull(dispatcher.LastPresenceNotification);
        Assert.Equal("online", dispatcher.LastPresenceNotification!.Status);
        Assert.Equal(recipientUserId, dispatcher.LastPresenceNotification.RecipientUserIds.Single());
    }

    [Fact]
    public async Task Handle_WhenStatusIsInvalid_ThrowsInvalidOperationException()
    {
        var dispatcher = new CapturingRealtimeClientDispatcher();
        var handler = new PublishPresenceChangeCommandHandler(dispatcher);

        await Assert.ThrowsAsync<InvalidOperationException>(() => handler.Handle(
            new PublishPresenceChangeCommand(Guid.NewGuid(), "busy", DateTime.UtcNow, [Guid.NewGuid()]),
            CancellationToken.None));

        Assert.Null(dispatcher.LastPresenceNotification);
    }
}
