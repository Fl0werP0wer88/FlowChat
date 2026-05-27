using EFCore.BulkExtensions;
using FlowChat.Core.Results;
using FlowChat.Shared.Application;
using Microsoft.EntityFrameworkCore;

namespace FlowChat.Shared.Persistance.BulkUpsert;

public sealed class EfCoreBulkUpsertExecutor<TDbContext, TItem>
    : IBulkUpsertExecutor<TItem>
    where TDbContext : DbContext
    where TItem : class
{
    private readonly TDbContext _dbContext;
    private readonly EfCoreBulkUpsertOptions<TItem> _options;

    public EfCoreBulkUpsertExecutor(
        TDbContext dbContext,
        EfCoreBulkUpsertOptions<TItem> options)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    public async Task<FlowChatResult<BulkUpsertCommandResult>> UpsertAsync(
        IReadOnlyCollection<TItem> items,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(items);
        cancellationToken.ThrowIfCancellationRequested();

        if (items.Count == 0)
        {
            return FlowChatResult<BulkUpsertCommandResult>.Success(BulkUpsertCommandResult.Empty);
        }

        var upsertItems = items as IList<TItem> ?? items.ToList();

        await _dbContext.BulkInsertOrUpdateAsync(
            upsertItems,
            _options.CreateBulkConfig(),
            cancellationToken: cancellationToken);

        return FlowChatResult<BulkUpsertCommandResult>.Success(
            BulkUpsertCommandResult.FromRequestedCount(items.Count));
    }
}
