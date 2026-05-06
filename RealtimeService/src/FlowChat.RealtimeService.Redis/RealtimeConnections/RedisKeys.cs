namespace FlowChat.RealtimeService.Redis.RealtimeConnections;

public static class RedisKeys
{
    public static string GetConnectionKey(string keyPrefix, string connectionId) => $"{keyPrefix}:connections:{connectionId}";

    public static string GetUserConnectionsKey(string keyPrefix, Guid userId) => $"{keyPrefix}:user-connections:{userId:D}";

    public static string GetUserInstancesKey(string keyPrefix, Guid userId) => $"{keyPrefix}:user-instances:{userId:D}";

    public static string GetUserInstanceCountsKey(string keyPrefix, Guid userId) => $"{keyPrefix}:user-instance-counts:{userId:D}";
}
