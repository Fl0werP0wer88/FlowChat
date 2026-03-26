using CSharpFunctionalExtensions;
using FlowChat.Domain.Abstractions;

namespace FlowChat.Core.Results;

public readonly struct FlowChatResult<TValue> :
    IFlowChatResult<TValue>,
    IFlowChatResultFactory<FlowChatResult<TValue>>
{
    private readonly Result<TValue, IDomainError> _innerResult;

    private FlowChatResult(Result<TValue, IDomainError> innerResult)
    {
        _innerResult = innerResult;
    }

    public bool IsSuccess => _innerResult.IsSuccess;

    public bool IsFailure => _innerResult.IsFailure;

    public TValue Value => _innerResult.Value;

    public IDomainError Error => _innerResult.Error;

    public static FlowChatResult<TValue> Success(TValue value)
    {
        return new FlowChatResult<TValue>(Result.Success<TValue, IDomainError>(value));
    }

    public static FlowChatResult<TValue> Failure(IDomainError error)
    {
        return new FlowChatResult<TValue>(Result.Failure<TValue, IDomainError>(error));
    }

    public static FlowChatResult<TValue> From(Result<TValue, IDomainError> result)
    {
        return new FlowChatResult<TValue>(result);
    }

    public static implicit operator FlowChatResult<TValue>(TValue value)
    {
        return Success(value);
    }

    public static implicit operator FlowChatResult<TValue>(Result<TValue, IDomainError> result)
    {
        return From(result);
    }

    public override string ToString() => _innerResult.ToString();
}
