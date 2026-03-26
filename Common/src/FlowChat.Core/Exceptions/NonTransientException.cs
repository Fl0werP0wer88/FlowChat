using System.Runtime.Serialization;

namespace FlowChat.Core.Exceptions;

[Serializable]
public class NonTransientException : FlowChatException
{
    public NonTransientException()
    {
    }

    public NonTransientException(string message)
        : base(message)
    {
    }

    public NonTransientException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

#pragma warning disable SYSLIB0051
    protected NonTransientException(SerializationInfo info, StreamingContext context)
        : base(info, context)
    {
    }
#pragma warning restore SYSLIB0051
}
