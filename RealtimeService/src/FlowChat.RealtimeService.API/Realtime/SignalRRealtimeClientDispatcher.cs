using FlowChat.RealtimeService.Application.Contracts.Infrastructure;
using Microsoft.AspNetCore.SignalR;

namespace FlowChat.RealtimeService.Api.Realtime;

public sealed class SignalRRealtimeClientDispatcher(IHubContext<ChatHub, IRealtimeClient> hubContext)
    : IRealtimeClientDispatcher
{
    private readonly IHubContext<ChatHub, IRealtimeClient> _hubContext = hubContext
        ?? throw new ArgumentNullException(nameof(hubContext));

    public Task ReceiveMessageAsync(ChatMessageParam notification, CancellationToken cancellationToken)
    {
        var groups = GetRecipientGroups(notification.RecipientUserIds);
        if (groups.Length == 0)
        {
            return Task.CompletedTask;
        }

        return _hubContext.Clients.Groups(groups).ReceiveMessage(new ChatMessageNotificationDto
        {
            MessageId = notification.MessageId,
            ConversationId = notification.ConversationId,
            SenderUserId = notification.SenderUserId,
            SenderDisplayName = notification.SenderDisplayName,
            Text = notification.Text,
            SequenceNum = notification.SequenceNum,
            SentAtUtc = notification.SentAtUtc,
            DeliveredAtUtc = notification.DeliveredAtUtc
        });
    }

    public Task PresenceChangedAsync(PresenceChangedParam notification, CancellationToken cancellationToken)
    {
        var groups = GetRecipientGroups(notification.RecipientUserIds);
        if (groups.Length == 0)
        {
            return Task.CompletedTask;
        }

        return _hubContext.Clients.Groups(groups).PresenceChanged(new PresenceDto
        {
            UserId = notification.UserId,
            Status = notification.Status,
            ChangedAtUtc = notification.ChangedAtUtc
        });
    }

    public Task GroupConversationChangedAsync(GroupConversationChangedParam notification, CancellationToken cancellationToken)
    {
        var groups = GetRecipientGroups(notification.ParticipantUserIds);
        if (groups.Length == 0)
        {
            return Task.CompletedTask;
        }

        return _hubContext.Clients.Groups(groups).GroupConversationChanged(new GroupConversationChangedDto
        {
            ConversationId = notification.ConversationId,
            Type = notification.Type,
            Name = notification.Name,
            CreatedByUserId = notification.CreatedByUserId,
            ParticipantUserIds = notification.ParticipantUserIds
        });
    }

    private static string[] GetRecipientGroups(IReadOnlyCollection<Guid> recipientUserIds) =>
        recipientUserIds
            .Where(userId => userId != Guid.Empty)
            .Distinct()
            .Select(GroupNames.ForUser)
            .ToArray();
}
