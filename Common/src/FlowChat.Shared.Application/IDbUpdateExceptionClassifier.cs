using Microsoft.EntityFrameworkCore;

namespace FlowChat.Shared.Application;

public interface IDbUpdateExceptionClassifier
{
    bool IsExpectedIdempotencyConflict(
        DbUpdateException exception,
        string idempotencyConflictKey);
}
