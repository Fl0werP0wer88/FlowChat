namespace FlowChat.Shared.Domain;

public interface IDomainError
{
    public bool IsTransient { get; }

    public bool IsIsolable { get; }

    string? ErrorMessage { get; }

    ErrorType ErrorType { get; }

    List<string>? Errors { get; }
}

