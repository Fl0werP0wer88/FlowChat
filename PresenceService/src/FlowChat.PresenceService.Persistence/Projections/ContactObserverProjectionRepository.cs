using System.Data.Common;
using FlowChat.Core.Exceptions;
using FlowChat.PresenceService.Application.Features.ContactObserverProjections;
using FlowChat.Shared.Application;
using Microsoft.EntityFrameworkCore;

namespace FlowChat.PresenceService.Persistence.Projections;

public sealed class ContactObserverProjectionRepository(AppDbContext dbContext)
    : IProjectionSingleRepository<ContactObserverProjectionDto>
{
    public async Task UpsertOrSoftDeleteAsync(
        ProjectionCommandItem<ContactObserverProjectionDto> item,
        CancellationToken cancellationToken)
    {
        var value = item.Value;

        try
        {
            await dbContext.Database.ExecuteSqlInterpolatedAsync(
                $"""
                INSERT INTO "ContactObserverReadModel"
                    ("ObservedUserId", "ObserverUserId", "IsBlocked", "SourceVersion",
                     "SourceCreatedAtUtc", "SourceLastModifiedAtUtc", "SourceDeletedAtUtc")
                VALUES
                    ({value.ObservedUserId}, {value.ObserverUserId}, {value.IsBlocked}, {item.SourceVersion},
                     {item.SourceCreatedAtUtc}, {item.SourceLastModifiedAtUtc}, {item.SourceDeletedAtUtc})
                ON CONFLICT ("ObservedUserId", "ObserverUserId") DO UPDATE SET
                    "IsBlocked" = EXCLUDED."IsBlocked",
                    "SourceVersion" = EXCLUDED."SourceVersion",
                    "SourceCreatedAtUtc" = EXCLUDED."SourceCreatedAtUtc",
                    "SourceLastModifiedAtUtc" = EXCLUDED."SourceLastModifiedAtUtc",
                    "SourceDeletedAtUtc" = EXCLUDED."SourceDeletedAtUtc"
                WHERE EXCLUDED."SourceVersion" > "ContactObserverReadModel"."SourceVersion";
                """,
                cancellationToken);
        }
        catch (DbException exception) when (exception.IsTransient)
        {
            throw new TransientException("Contact observer projection upsert failed transiently.", exception);
        }
        catch (DbException exception)
        {
            throw new IsolableException("Contact observer projection upsert failed for the consumed item.", exception);
        }
    }
}
