namespace FlowChat.RealtimeService.Infrastructure.Realtime;

public static class GroupNames
{
    public static string ForUser(Guid userId) => $"user:{userId}";
}
