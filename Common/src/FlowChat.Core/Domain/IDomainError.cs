namespace FlowChat.Shared.Domain;

public interface IDomainError
{
    string? ErrorMessage { get; init; }

    ErrorType ErrorType { get; init; }

    List<string>? Errors { get; init; }
}

