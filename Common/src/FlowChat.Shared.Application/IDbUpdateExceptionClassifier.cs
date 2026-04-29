using Microsoft.EntityFrameworkCore;

namespace FlowChat.Shared.Application;

public interface IDbUpdateExceptionClassifier
{
    bool IsExpectedUniqueConstraintViolation(
        DbUpdateException exception,
        IReadOnlyCollection<string> expectedConstraintNames);
}
