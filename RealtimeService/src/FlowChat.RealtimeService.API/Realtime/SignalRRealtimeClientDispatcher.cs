using FlowChat.RealtimeService.Application.Contracts.Infrastructure;
using FlowChat.RealtimeService.Api.Realtime.Notifications;
using Microsoft.AspNetCore.SignalR;

namespace FlowChat.RealtimeService.Api.Realtime;

public sealed class SignalRRealtimeClientDispatcher(IHubContext<ChatHub, IRealtimeClient> hubContext)
    : IRealtimeClientDispatcher
{
    private readonly IHubContext<ChatHub, IRealtimeClient> _hubContext = hubContext
        ?? throw new ArgumentNullException(nameof(hubContext));

    public Task MessageReceivedAsync(ChatMessageParam notification, CancellationToken cancellationToken) =>
        _hubContext.Clients.Group(GroupNames.ForConversation(notification.ConversationId)).MessageReceived(new ChatMessageReceivedNotification
        {
            MessageId = notification.MessageId,
            ConversationId = notification.ConversationId,
            SenderUserId = notification.SenderUserId,
            Text = notification.Text,
            SequenceNum = notification.SequenceNum,
            SentAtUtc = notification.SentAtUtc,
            DeliveredAtUtc = notification.DeliveredAtUtc
        });

    public Task PresenceChangedAsync(PresenceChangedParam notification, CancellationToken cancellationToken)
    {
        var groups = GetRecipientGroups(notification.RecipientUserIds);
        if (groups.Length == 0)
        {
            return Task.CompletedTask;
        }

        return _hubContext.Clients.Groups(groups).PresenceChanged(new PresenceChangedNotification
        {
            UserId = notification.UserId,
            Status = notification.Status,
            ChangedAtUtc = notification.ChangedAtUtc
        });
    }

    public Task GroupConversationChangedAsync(GroupConversationChangedParam notification, CancellationToken cancellationToken) =>
        _hubContext.Clients.Group(GroupNames.ForConversation(notification.ConversationId)).GroupConversationChanged(new GroupConversationChangedNotification
        {
            ConversationId = notification.ConversationId,
            Type = notification.Type,
            Name = notification.Name,
            CreatedByUserId = notification.CreatedByUserId
        });

    public Task ConversationParticipantsAddedAsync(ConversationParticipantsAddedParam notification, CancellationToken cancellationToken) =>
        _hubContext.Clients.Group(GroupNames.ForConversation(notification.ConversationId)).ConversationParticipantsAdded(new ConversationParticipantsAddedNotification
        {
            ConversationId = notification.ConversationId,
            ConversationType = notification.ConversationType,
            ParticipantUserIds = notification.ParticipantUserIds
        });

    public Task ConversationParticipantsRemovedAsync(ConversationParticipantsRemovedParam notification, CancellationToken cancellationToken) =>
        _hubContext.Clients.Group(GroupNames.ForConversation(notification.ConversationId)).ConversationParticipantsRemoved(new ConversationParticipantsRemovedNotification
        {
            ConversationId = notification.ConversationId,
            ConversationType = notification.ConversationType,
            ParticipantUserIds = notification.ParticipantUserIds
        });

    private static string[] GetRecipientGroups(IReadOnlyCollection<Guid> recipientUserIds) =>
        recipientUserIds
            .Where(userId => userId != Guid.Empty)
            .Distinct()
            .Select(GroupNames.ForUser)
            .ToArray();
}
