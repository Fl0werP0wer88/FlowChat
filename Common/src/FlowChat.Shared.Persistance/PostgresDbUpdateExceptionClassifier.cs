using FlowChat.Shared.Application;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace FlowChat.Shared.Persistance;

public sealed class PostgresDbUpdateExceptionClassifier : IDbUpdateExceptionClassifier
{
    public bool IsExpectedUniqueConstraintViolation(
        DbUpdateException exception,
        IReadOnlyCollection<string> expectedConstraintNames)
    {
        ArgumentNullException.ThrowIfNull(exception);
        ArgumentNullException.ThrowIfNull(expectedConstraintNames);

        if (exception.InnerException is not PostgresException postgresException
            || postgresException.SqlState != PostgresErrorCodes.UniqueViolation)
        {
            return false;
        }

        return expectedConstraintNames.Count == 0
            || expectedConstraintNames.Contains(postgresException.ConstraintName, StringComparer.Ordinal);
    }
}
