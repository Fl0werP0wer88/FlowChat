using FlowChat.RealtimeService.Application.Contracts.Infrastructure;
using Microsoft.AspNetCore.SignalR;

namespace FlowChat.RealtimeService.Api.Realtime;

public sealed class SignalRRealtimeGroupManager(IHubContext<ChatHub, IRealtimeClient> hubContext)
    : IRealtimeGroupManager
{
    private readonly IHubContext<ChatHub, IRealtimeClient> _hubContext = hubContext
        ?? throw new ArgumentNullException(nameof(hubContext));

    public Task AddToUserGroupAsync(string connectionId, Guid userId, CancellationToken cancellationToken) =>
        _hubContext.Groups.AddToGroupAsync(connectionId, GroupNames.ForUser(userId), cancellationToken);

    public Task RemoveFromUserGroupAsync(string connectionId, Guid userId, CancellationToken cancellationToken) =>
        _hubContext.Groups.RemoveFromGroupAsync(connectionId, GroupNames.ForUser(userId), cancellationToken);

    public Task AddToConversationGroupAsync(string connectionId, Guid conversationId, CancellationToken cancellationToken) =>
        _hubContext.Groups.AddToGroupAsync(connectionId, GroupNames.ForConversation(conversationId), cancellationToken);

    public Task RemoveFromConversationGroupAsync(string connectionId, Guid conversationId, CancellationToken cancellationToken) =>
        _hubContext.Groups.RemoveFromGroupAsync(connectionId, GroupNames.ForConversation(conversationId), cancellationToken);
}
