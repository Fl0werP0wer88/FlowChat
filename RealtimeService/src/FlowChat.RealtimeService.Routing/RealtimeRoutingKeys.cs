namespace FlowChat.RealtimeService.Routing;

public static class RealtimeRoutingKeys
{
    public static string GetUserInstancesKey(string keyPrefix, Guid userId) => $"{keyPrefix}:user-instances:{userId:D}";

    public static string GetUserInstanceCountsKey(string keyPrefix, Guid userId) => $"{keyPrefix}:user-instance-counts:{userId:D}";
}
