namespace FlowChat.Shared.Application;

public interface IProjectionCommandItem<TValue>
    where TValue : class
{
    TValue? Value { get; }
    int SourceVersion { get; }
    DateTimeOffset SourceCreatedAtUtc { get; }
    DateTimeOffset SourceLastModifiedAtUtc { get; }
    DateTimeOffset? SourceDeletedAtUtc { get; }
}

public sealed record ProjectionCommandItem<TValue>(
    Guid Id,
    TValue? Value,
    int SourceVersion,
    DateTimeOffset SourceCreatedAtUtc,
    DateTimeOffset SourceLastModifiedAtUtc,
    DateTimeOffset? SourceDeletedAtUtc)
    : IProjectionCommandItem<TValue>
    where TValue : class;
