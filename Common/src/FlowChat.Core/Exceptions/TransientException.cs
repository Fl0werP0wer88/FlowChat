using System.Runtime.Serialization;

namespace FlowChat.Core.Exceptions;

[Serializable]
public class TransientException : FlowChatException
{
    public TransientException()
    {
    }

    public TransientException(string message)
        : base(message)
    {
    }

    public TransientException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

#pragma warning disable SYSLIB0051
    protected TransientException(SerializationInfo info, StreamingContext context)
        : base(info, context)
    {
    }
#pragma warning restore SYSLIB0051
}
