namespace FlowChat.Shared.Domain;

public record DomainError : IDomainError
{
    public static DomainError Conflict(string? message = "The data provided conflicts with existing data.") =>
        new(message ?? "The data provided conflicts with existing data.", ErrorType.Conflict);

    public static DomainError NotFound(string? message = "The requested item could not be found.", FailureKind failureKind = FailureKind.None) =>
        new(message ?? "The requested item could not be found.", ErrorType.NotFound, failureKind: failureKind);

    public static DomainError BadRequest(string? message = "Invalid request or parameters.") =>
        new(message ?? "Invalid request or parameters.", ErrorType.BadRequest);

    public static DomainError OperationCanceled(string? message = "The request was canceled.") =>
        new(message ?? "The request was canceled.", ErrorType.OperationCanceled);

    public static DomainError Validation(string? message = "Validation Failed.", List<string>? errors = null) =>
        new(message ?? "Validation Failed.", ErrorType.Validation, errors);

    public static DomainError UnExpected(string? message = "Unexpected error happened.", FailureKind failureKind = FailureKind.None) =>
        new(message ?? "Something when wrong.", ErrorType.Unexpected, failureKind: failureKind);

    public static DomainError Unauthorized(string? message = "Unauthorized.") =>
        new(message ?? "Unauthorized.", ErrorType.Unauthorized);

    private DomainError(string? message, ErrorType errorType, List<string>? errors = null, FailureKind failureKind = FailureKind.None)
    {
        ErrorMessage = message;
        ErrorType = errorType;
        Errors = errors ?? [];
        FailureKind = failureKind;
    }

    public FailureKind FailureKind { get; init; }

    public string? ErrorMessage { get; init; }

    public ErrorType ErrorType { get; init; }

    public List<string>? Errors { get; init; }
}

