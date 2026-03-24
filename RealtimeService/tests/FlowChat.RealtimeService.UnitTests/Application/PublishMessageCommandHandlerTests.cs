using FlowChat.Domain.Abstractions;
using FlowChat.RealtimeService.Application.Features.Messages.Commands.PublishMessage;

namespace FlowChat.RealtimeService.UnitTests;

public sealed class PublishMessageCommandHandlerTests
{
    [Fact]
    public async Task Handle_MapsNotificationAndDispatchesToRecipients()
    {
        var dispatcher = new CapturingRealtimeClientDispatcher();
        var handler = new PublishMessageCommandHandler(dispatcher);
        var recipientUserId = Guid.NewGuid();

        var result = await handler.Handle(
            new PublishMessageCommand(
                Guid.NewGuid(),
                Guid.NewGuid(),
                Guid.NewGuid(),
                " John Doe ",
                " Hello there ",
                new DateTime(2026, 3, 17, 12, 0, 0, DateTimeKind.Utc),
                [recipientUserId, recipientUserId, Guid.Empty]),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(dispatcher.LastMessageNotification);
        Assert.Equal("John Doe", dispatcher.LastMessageNotification!.SenderDisplayName);
        Assert.Equal("Hello there", dispatcher.LastMessageNotification.Text);
        Assert.Single(dispatcher.LastMessageNotification.RecipientUserIds);
        Assert.Equal(recipientUserId, dispatcher.LastMessageNotification.RecipientUserIds.Single());
    }

    [Fact]
    public async Task Handle_WhenRecipientsMissing_ReturnsBadRequestFailure()
    {
        var dispatcher = new CapturingRealtimeClientDispatcher();
        var handler = new PublishMessageCommandHandler(dispatcher);

        var result = await handler.Handle(
            new PublishMessageCommand(
                Guid.NewGuid(),
                Guid.NewGuid(),
                Guid.NewGuid(),
                "John Doe",
                "Hello",
                DateTime.UtcNow,
                [Guid.Empty]),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.BadRequest, result.Error.ErrorType);
        Assert.Null(dispatcher.LastMessageNotification);
    }
}
