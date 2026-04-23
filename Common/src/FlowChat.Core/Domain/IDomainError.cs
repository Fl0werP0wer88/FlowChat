namespace FlowChat.Shared.Domain;

public interface IDomainError
{
    public bool IsTransient { get; }
    string? ErrorMessage { get; }

    ErrorType ErrorType { get; }

    List<string>? Errors { get; }
}

