using Ardalis.SmartEnum;

namespace FlowChat.Shared.Domain;

public abstract class ErrorType(string name, int value) : SmartEnum<ErrorType>(name, value)
{
    public static readonly ErrorType Conflict = new ConflictEnum();
    public static readonly ErrorType NotFound = new NotFoundEnum();
    public static readonly ErrorType BadRequest = new BadRequestEnum();
    public static readonly ErrorType Validation = new ValidationEnum();
    public static readonly ErrorType Unexpected = new UnexpectedEnum();
    public static readonly ErrorType Unauthorized = new UnauthorizedEnum();

    private sealed class ConflictEnum : ErrorType
    {
        public ConflictEnum() : base("Conflict", 0)
        {
        }
    }

    private sealed class NotFoundEnum : ErrorType
    {
        public NotFoundEnum() : base("NotFound", 1)
        {
        }
    }

    private sealed class BadRequestEnum : ErrorType
    {
        public BadRequestEnum() : base("BadRequest", 2)
        {
        }
    }

    private sealed class ValidationEnum : ErrorType
    {
        public ValidationEnum() : base("Validation", 3)
        {
        }
    }

    private sealed class UnexpectedEnum : ErrorType
    {
        public UnexpectedEnum() : base("Unexpected", 4)
        {
        }
    }

    private sealed class UnauthorizedEnum : ErrorType
    {
        public UnauthorizedEnum() : base("Unauthorized", 5)
        {
        }
    }
}

