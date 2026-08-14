using FlowChat.Shared.Persistance;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Silverback;
using Silverback.Storage;

namespace FlowChat.Shared.Infrastructure.Silverback.Persistence;

// This variant coordinates application writes with the Silverback outbox in API and worker hosts
public class SilverbackEfUnitOfWork<TDbContext>(
    TDbContext dbContext,
    ISilverbackContext silverbackContext)
    : EfUnitOfWork<TDbContext>(dbContext)
    where TDbContext : DbContext
{
    // Silverback must share the EF transaction so business data and the outbox commit atomically
    protected override void EnrichTransaction(IDbContextTransaction transaction) =>
        silverbackContext.EnlistDbTransaction(transaction.GetDbTransaction(), ownTransaction: false);

    // Clear the storage transaction even after failures so the scoped context is reusable
    protected override void OnFinally() =>
        silverbackContext.ClearStorageTransaction();
}
