namespace FlowChat.RealtimeService.Api.Realtime;

public static class GroupNames
{
    public static string ForUser(Guid userId) => $"user:{userId:D}";
}
