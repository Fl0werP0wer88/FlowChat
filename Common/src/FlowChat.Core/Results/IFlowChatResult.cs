using FlowChat.Shared.Domain;

namespace FlowChat.Core.Results;

public interface IFlowChatResult
{
    bool IsSuccess { get; }

    bool IsFailure { get; }

    IDomainError Error { get; }
}

