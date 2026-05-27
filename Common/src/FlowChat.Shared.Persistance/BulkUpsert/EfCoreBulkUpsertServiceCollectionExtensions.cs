using FlowChat.Shared.Application;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace FlowChat.Shared.Persistance.BulkUpsert;

public static class EfCoreBulkUpsertServiceCollectionExtensions
{
    public static IServiceCollection AddEfCoreBulkUpsertExecutor<TDbContext, TItem>(
        this IServiceCollection services,
        Action<EfCoreBulkUpsertOptions<TItem>>? configure = null)
        where TDbContext : DbContext
        where TItem : class
    {
        var options = new EfCoreBulkUpsertOptions<TItem>();
        configure?.Invoke(options);

        services.AddSingleton(options);
        services.AddScoped<IBulkUpsertExecutor<TItem>, EfCoreBulkUpsertExecutor<TDbContext, TItem>>();

        return services;
    }
}
