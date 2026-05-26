using FlowChat.Shared.Application;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace FlowChat.Shared.Persistance;

public sealed class PostgresDbUpdateExceptionClassifier(PostgresDbUpdateExceptionClassifierOptions options)
    : IDbUpdateExceptionClassifier
{
    public bool IsIdempotencyConflict(
        DbUpdateException exception,
        string idempotencyConflictKey)
    {
        ArgumentNullException.ThrowIfNull(exception);
        ArgumentException.ThrowIfNullOrWhiteSpace(idempotencyConflictKey);

        if (exception.InnerException is not PostgresException postgresException
            || postgresException.SqlState != PostgresErrorCodes.UniqueViolation)
        {
            return false;
        }

        return options.UniqueConstraintNamesByIdempotencyConflictKey.TryGetValue(
                idempotencyConflictKey,
                out var constraintNames)
            && constraintNames.Contains(postgresException.ConstraintName, StringComparer.Ordinal);
    }
}
