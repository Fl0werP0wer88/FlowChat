using FlowChat.RealtimeService.Application.Messages.Commands.ReceiveMessage;

namespace FlowChat.RealtimeService.UnitTests;

public sealed class ReceiveMessageCommandHandlerTests
{
    [Fact]
    public async Task Handle_MapsNotificationAndDispatchesToRecipients()
    {
        var dispatcher = new CapturingRealtimeClientDispatcher();
        var handler = new ReceiveMessageCommandHandler(dispatcher);
        var recipientUserId = Guid.NewGuid();

        await handler.Handle(
            new ReceiveMessageCommand(
                Guid.NewGuid(),
                Guid.NewGuid(),
                Guid.NewGuid(),
                " John Doe ",
                " Hello there ",
                new DateTime(2026, 3, 17, 12, 0, 0, DateTimeKind.Utc),
                [recipientUserId, recipientUserId, Guid.Empty]),
            CancellationToken.None);

        Assert.NotNull(dispatcher.LastMessageNotification);
        Assert.Equal("John Doe", dispatcher.LastMessageNotification!.SenderDisplayName);
        Assert.Equal("Hello there", dispatcher.LastMessageNotification.Text);
        Assert.Single(dispatcher.LastMessageNotification.RecipientUserIds);
        Assert.Equal(recipientUserId, dispatcher.LastMessageNotification.RecipientUserIds.Single());
    }

    [Fact]
    public async Task Handle_WhenRecipientsMissing_ThrowsInvalidOperationException()
    {
        var dispatcher = new CapturingRealtimeClientDispatcher();
        var handler = new ReceiveMessageCommandHandler(dispatcher);

        await Assert.ThrowsAsync<InvalidOperationException>(() => handler.Handle(
            new ReceiveMessageCommand(
                Guid.NewGuid(),
                Guid.NewGuid(),
                Guid.NewGuid(),
                "John Doe",
                "Hello",
                DateTime.UtcNow,
                [Guid.Empty]),
            CancellationToken.None));

        Assert.Null(dispatcher.LastMessageNotification);
    }
}
