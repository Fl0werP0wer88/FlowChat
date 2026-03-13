using FlowChat.Domain.Abstractions;

namespace FlowChat.Application.Abstractions;

public sealed class ValidationErrorCollector
{
    private readonly List<string> _errors = [];

    public bool HasErrors => _errors.Count > 0;

    public ValidationErrorCollector AddIf(bool condition, string error)
    {
        if (condition)
        {
            _errors.Add(error);
        }

        return this;
    }

    public Result<TResponse, IDomainError> ToFailure<TResponse>(string message = "Validation Failed.")
    {
        return Result.Failure<TResponse, IDomainError>(
            DomainError.Validation(message, [.. _errors]));
    }
}
