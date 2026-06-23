namespace FlowChat.Core.Messaging.UserProfileService.Events;

public sealed class UserProfileEmail
{
    public required string Address { get; init; }
    public bool IsConfirmed { get; init; }
    public bool IsVisible { get; init; }
}
