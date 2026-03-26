using FlowChat.Domain.Abstractions;

namespace FlowChat.Core.Results;

public interface IFlowChatResult<out TValue> : IFlowChatResult
{
    TValue Value { get; }
}

public interface IFlowChatResultFactory<TSelf>
    where TSelf : IFlowChatResultFactory<TSelf>
{
    static abstract TSelf Failure(IDomainError error);
}
