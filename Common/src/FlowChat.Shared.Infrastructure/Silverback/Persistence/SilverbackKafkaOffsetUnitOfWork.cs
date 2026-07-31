using FlowChat.Shared.Application;
using Microsoft.EntityFrameworkCore;
using Silverback;
using Silverback.Messaging.Consuming.KafkaOffsetStore;

namespace FlowChat.Shared.Infrastructure.Silverback.Persistence;

// Consumer workers additionally persist the consumed offset in the shared transaction
public sealed class SilverbackKafkaOffsetUnitOfWork<TDbContext>
    : SilverbackEfUnitOfWork<TDbContext>,
        IConsumedOffsetCommitter
    where TDbContext : DbContext
{
    private readonly ISilverbackContext _silverbackContext;

    public SilverbackKafkaOffsetUnitOfWork(
        TDbContext dbContext,
        ISilverbackContext silverbackContext)
        : base(dbContext, silverbackContext)
    {
        _silverbackContext = silverbackContext;
    }

    public Task CommitConsumedOffsetsAsync(CancellationToken cancellationToken) =>
        _silverbackContext.GetKafkaOffsetStoreScope().StoreOffsetsAsync();
}
