namespace FlowChat.Shared.Persistance;

public sealed class PostgresDbUpdateExceptionClassifierOptions
{
    public IDictionary<string, IReadOnlyCollection<string>> UniqueConstraintNamesByIdempotencyConflictKey { get; } =
        new Dictionary<string, IReadOnlyCollection<string>>(StringComparer.Ordinal);
}
