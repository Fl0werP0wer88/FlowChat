using System.Runtime.Serialization;

namespace FlowChat.Core.Exceptions;

[Serializable]
public class FlowChatException : Exception
{
    public FlowChatException()
    {
    }

    public FlowChatException(string message)
        : base(message)
    {
    }

    public FlowChatException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

#pragma warning disable SYSLIB0051
    protected FlowChatException(SerializationInfo info, StreamingContext context)
        : base(info, context)
    {
    }
#pragma warning restore SYSLIB0051
}
