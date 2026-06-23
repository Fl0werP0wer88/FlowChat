using System.Runtime.Serialization;
using FlowChat.Core.Exceptions;

namespace FlowChat.Shared.Domain.Exceptions;

[Serializable]
public class ValidationException : FlowChatException
{
    public IReadOnlyList<string> Errors { get; }

    public ValidationException(IEnumerable<string> errors)
        : base("Validation failed.")
    {
        Errors = errors?.ToList() ?? [];
    }

    public ValidationException(string error)
        : base(error)
    {
        Errors = string.IsNullOrWhiteSpace(error) ? [] : [error];
    }

    public ValidationException(string message, Exception innerException)
        : base(message, innerException)
    {
        Errors = string.IsNullOrWhiteSpace(innerException.Message) ? [] : [innerException.Message];
    }

#pragma warning disable SYSLIB0051
    protected ValidationException(SerializationInfo info, StreamingContext context)
        : base(info, context)
    {
        Errors = (IReadOnlyList<string>?)info.GetValue(nameof(Errors), typeof(IReadOnlyList<string>)) ?? [];
    }

    [Obsolete("Formatter-based serialization is obsolete.")]
    public override void GetObjectData(SerializationInfo info, StreamingContext context)
    {
        base.GetObjectData(info, context);
        info.AddValue(nameof(Errors), Errors, typeof(IReadOnlyList<string>));
    }
#pragma warning restore SYSLIB0051
}

