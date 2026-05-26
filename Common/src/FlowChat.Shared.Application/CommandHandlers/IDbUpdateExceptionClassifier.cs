using Microsoft.EntityFrameworkCore;

namespace FlowChat.Shared.Application;

public interface IDbUpdateExceptionClassifier
{
    bool IsIdempotencyConflict(
        DbUpdateException exception,
        string idempotencyConflictKey);
}
