using StackExchange.Redis;

namespace FlowChat.Shared.Infrastructure.Redis;

/// <summary>
/// Provides Redis repositories with access to the ambient transaction so they can queue commands
/// on the active ITransaction during ExecuteInTransactionAsync, or fall back to a plain IDatabase.
/// </summary>
public interface IRedisTransactionContext
{
    IDatabaseAsync GetActiveDatabase();
}
