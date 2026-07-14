using System.Runtime.Serialization;
using FlowChat.Core.Results;

namespace FlowChat.Core.Exceptions;

[Serializable]
public class ResultException : FlowChatException
{
    public ResultException(FlowChatResult result)
    {
        Result = result;
    }

    public ResultException(FlowChatResult result, string message)
        : base(message)
    {
        Result = result;
    }

    public ResultException(FlowChatResult result, string message, Exception innerException)
        : base(message, innerException)
    {
        Result = result;
    }

    public FlowChatResult Result { get; }

#pragma warning disable SYSLIB0051
    protected ResultException(SerializationInfo info, StreamingContext context)
        : base(info, context)
    {
        Result = (FlowChatResult)info.GetValue(nameof(Result), typeof(FlowChatResult))!;
    }

    [Obsolete("This API supports obsolete formatter-based serialization.", DiagnosticId = "SYSLIB0051")]
    public override void GetObjectData(SerializationInfo info, StreamingContext context)
    {
        base.GetObjectData(info, context);
        info.AddValue(nameof(Result), Result);
    }
#pragma warning restore SYSLIB0051
}
