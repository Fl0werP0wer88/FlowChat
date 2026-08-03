namespace FlowChat.Shared.Application;

public sealed record ProjectionSingleCommand<TValue>(
    ProjectionCommandItem<TValue> Item) : ICommand<MediatR.Unit>
    where TValue : class;
