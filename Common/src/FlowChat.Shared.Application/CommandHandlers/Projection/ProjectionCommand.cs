namespace FlowChat.Shared.Application;

public sealed record ProjectionCommand<TValue>(
    ProjectionCommandItem<TValue> Item) : ICommand<MediatR.Unit>
    where TValue : class;
