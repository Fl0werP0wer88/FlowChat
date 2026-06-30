using FlowChat.Core.Messaging;

namespace FlowChat.Shared.Application;

public interface IProjectionCommandItem<TValue>
    where TValue : class
{
    TValue Value { get; }
    OperationType Operation { get; }
    int SourceVersion { get; }
    DateTimeOffset SourceCreatedAtUtc { get; }
    DateTimeOffset SourceLastModifiedAtUtc { get; }
    DateTimeOffset? SourceDeletedAtUtc { get; }
}

public sealed record ProjectionCommandItem<TValue>(
    TValue Value,
    OperationType Operation,
    int SourceVersion,
    DateTimeOffset SourceCreatedAtUtc,
    DateTimeOffset SourceLastModifiedAtUtc,
    DateTimeOffset? SourceDeletedAtUtc)
    : IProjectionCommandItem<TValue>
    where TValue : class;
