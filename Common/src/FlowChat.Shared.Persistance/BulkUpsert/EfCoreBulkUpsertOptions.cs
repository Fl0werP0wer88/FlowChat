using EFCore.BulkExtensions;

namespace FlowChat.Shared.Persistance.BulkUpsert;

public sealed class EfCoreBulkUpsertOptions<TItem>
    where TItem : class
{
    public Action<BulkConfig>? ConfigureBulkConfig { get; set; }

    internal BulkConfig CreateBulkConfig()
    {
        var bulkConfig = new BulkConfig();
        ConfigureBulkConfig?.Invoke(bulkConfig);

        return bulkConfig;
    }
}
