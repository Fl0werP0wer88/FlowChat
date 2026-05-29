using FlowChat.Shared.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace FlowChat.Shared.Persistance.Auditing;

public sealed class EntityBaseSaveChangesInterceptor : SaveChangesInterceptor
{
    private const string SystemActor = "system";

    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        ApplyAuditInfo(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        ApplyAuditInfo(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private static void ApplyAuditInfo(DbContext? dbContext)
    {
        if (dbContext is null)
        {
            return;
        }

        foreach (var entry in dbContext.ChangeTracker.Entries<IAuditableEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.SetCreated(SystemActor);
                    entry.Entity.SetUpdated(SystemActor);
                    break;
                case EntityState.Modified:
                    entry.Entity.SetUpdated(SystemActor);
                    break;
            }
        }
    }
}

