using System.Data.Common;
using Microsoft.EntityFrameworkCore;

namespace FlowChat.Shared.Application;

public static class DbUpdateExceptionExtensions
{
    private const string PostgresUniqueViolationSqlState = "23505";

    public static bool IsUniqueConstraintViolation(this DbUpdateException exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        return exception.InnerException is DbException dbException
            && dbException.SqlState == PostgresUniqueViolationSqlState;
    }
}
