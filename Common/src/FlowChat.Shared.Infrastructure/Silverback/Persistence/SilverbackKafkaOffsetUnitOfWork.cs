using FlowChat.Shared.Application;
using Microsoft.EntityFrameworkCore;
using Silverback;
using Silverback.Messaging.Consuming.KafkaOffsetStore;

namespace FlowChat.Shared.Infrastructure.Silverback.Persistence;
//ToDo: This one suppose to be used on Supcribers worker not api.Consider naming change.
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
