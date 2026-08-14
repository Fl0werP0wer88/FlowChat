using FlowChat.Core.Messaging;

namespace FlowChat.Shared.Application;

public sealed record ProjectionCommandItem<TValue>(
    TValue Value,
    OperationType Operation,
    int SourceVersion,
    DateTimeOffset SourceCreatedAtUtc,
    DateTimeOffset SourceLastModifiedAtUtc,
    DateTimeOffset? SourceDeletedAtUtc)
    : IProjectionCommandItem<TValue>
    where TValue : class;
