namespace FlowChat.Core.Messaging.AuthService.Events;

public sealed class Email
{
    public required string Address { get; init; }

    public bool IsAuth { get; init; }
}
