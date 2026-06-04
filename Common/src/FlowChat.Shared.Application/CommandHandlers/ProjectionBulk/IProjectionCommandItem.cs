namespace FlowChat.Shared.Application;

public interface IProjectionCommandItem<TValue>
    where TValue : class
{
    TValue? Value { get; }
    int SourceVersion { get; }
}
