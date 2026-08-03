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
