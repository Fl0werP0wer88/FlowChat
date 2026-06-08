using System.Runtime.Serialization;

namespace FlowChat.Core.Exceptions;

[Serializable]
public class IsolableException : FlowChatException
{
    public IsolableException()
    {
    }

    public IsolableException(string message)
        : base(message)
    {
    }

    public IsolableException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

#pragma warning disable SYSLIB0051
    protected IsolableException(SerializationInfo info, StreamingContext context)
        : base(info, context)
    {
    }
#pragma warning restore SYSLIB0051
}
