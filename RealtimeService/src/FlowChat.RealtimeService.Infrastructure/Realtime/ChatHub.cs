using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace FlowChat.RealtimeService.Infrastructure.Realtime;

[Authorize]
public sealed class ChatHub(ILogger<ChatHub> logger) : Hub<IRealtimeClient>
{
    private const string SubjectClaimType = "sub";
    private readonly ILogger<ChatHub> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    public override async Task OnConnectedAsync()
    {
        var userId = ResolveUserId();
        if (userId is null)
        {
            _logger.LogWarning("Rejecting realtime connection {ConnectionId} because authenticated user id is missing.", Context.ConnectionId);
            Context.Abort();
            return;
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, GroupNames.ForUser(userId.Value));
        await base.OnConnectedAsync();
    }

    private Guid? ResolveUserId()
    {
        var value = Context.User?.FindFirstValue(SubjectClaimType)
            ?? Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);

        return Guid.TryParse(value, out var userId) ? userId : null;
    }
}
