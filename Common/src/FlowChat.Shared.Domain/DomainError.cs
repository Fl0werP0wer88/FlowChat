namespace FlowChat.Shared.Domain;

public record DomainError : IDomainError
{
    public static DomainError Conflict(string? message = "The data provided conflicts with existing data.") =>
        new(message ?? "The data provided conflicts with existing data.", ErrorType.Conflict);

    public static DomainError ConcurrencyConflict(string? message = "The data was modified by another operation.") =>
        new(message ?? "The data was modified by another operation.", ErrorType.ConcurrencyConflict, isTransient: true);

    public static DomainError NotFound(string? message = "The requested item could not be found.", bool isTransient = false) =>
        new(message ?? "The requested item could not be found.", ErrorType.NotFound, isTransient: isTransient);

    public static DomainError BadRequest(string? message = "Invalid request or parameters.") =>
        new(message ?? "Invalid request or parameters.", ErrorType.BadRequest);

    public static DomainError Validation(string? message = "Validation Failed.", List<string>? errors = null) =>
        new(message ?? "Validation Failed.", ErrorType.Validation, errors);

    public static DomainError UnExpected(string? message = "Unexpected error happened.", bool isTransient = false) =>
        new(message ?? "Something when wrong.", ErrorType.Unexpected, isTransient: isTransient);

    public static DomainError Unauthorized(string? message = "Unauthorized.") =>
        new(message ?? "Unauthorized.", ErrorType.Unauthorized);

    private DomainError(string? message, ErrorType errorType, List<string>? errors = null, bool isTransient = false)
    {
        ErrorMessage = message;
        ErrorType = errorType;
        Errors = errors ?? [];
        IsTransient = isTransient;
    }

    public bool IsTransient { get; init; }

    public string? ErrorMessage { get; init; }

    public ErrorType ErrorType { get; init; }

    public List<string>? Errors { get; init; }
}

