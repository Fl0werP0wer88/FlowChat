using CSharpFunctionalExtensions;
using FlowChat.Shared.Domain;

namespace FlowChat.Core.Results;

public readonly struct FlowChatResult :
    IFlowChatResult,
    IFlowChatResultFactory<FlowChatResult>
{
    private readonly UnitResult<IDomainError> _innerResult;

    private FlowChatResult(UnitResult<IDomainError> innerResult)
    {
        _innerResult = innerResult;
    }

    public bool IsSuccess => _innerResult.IsSuccess;

    public bool IsFailure => _innerResult.IsFailure;

    public IDomainError Error => _innerResult.Error;

    public static FlowChatResult Success()
    {
        return new FlowChatResult(UnitResult.Success<IDomainError>());
    }

    public static FlowChatResult Failure(IDomainError error)
    {
        return new FlowChatResult(UnitResult.Failure(error));
    }

    public static FlowChatResult From(UnitResult<IDomainError> result)
    {
        return new FlowChatResult(result);
    }

    public override string ToString() => _innerResult.ToString();
}
