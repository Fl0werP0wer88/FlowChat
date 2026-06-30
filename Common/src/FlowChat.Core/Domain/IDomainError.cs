namespace FlowChat.Shared.Domain;

public interface IDomainError
{
    public FailureKind FailureKind { get; }

    string? ErrorMessage { get; }

    ErrorType ErrorType { get; }

    List<string>? Errors { get; }
}

