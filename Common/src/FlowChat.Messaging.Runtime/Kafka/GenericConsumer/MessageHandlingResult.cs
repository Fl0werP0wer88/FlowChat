namespace FlowChat.Messaging.Runtime.Kafka.GenericConsumer;

public enum MessageHandlingAction
{
    Success,
    Skip,
    Retry,
    DeadLetter
}

public readonly record struct MessageHandlingResult(MessageHandlingAction Action, string? Error)
{
    public static MessageHandlingResult Success() => new(MessageHandlingAction.Success, null);
    public static MessageHandlingResult Skip(string? reason = null) => new(MessageHandlingAction.Skip, reason);
    public static MessageHandlingResult Retry(string? error = null) => new(MessageHandlingAction.Retry, error);
    public static MessageHandlingResult DeadLetter(string? error = null) => new(MessageHandlingAction.DeadLetter, error);
}

