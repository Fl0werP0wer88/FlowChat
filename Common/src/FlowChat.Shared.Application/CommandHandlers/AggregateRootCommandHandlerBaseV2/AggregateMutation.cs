using FlowChat.Shared.Domain;

namespace FlowChat.Shared.Application.CommandHandlers.AggregateRootCommandHandlerBaseV2;

public sealed record AggregateMutation<TResponse>(
    TResponse Response,
    MutationType MutationType)
    where TResponse : notnull;
