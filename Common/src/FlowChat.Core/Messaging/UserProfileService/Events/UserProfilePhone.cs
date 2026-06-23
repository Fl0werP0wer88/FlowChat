namespace FlowChat.Core.Messaging.UserProfileService.Events;

public sealed class UserProfilePhone
{
    public required string Number { get; init; }
    public bool IsConfirmed { get; init; }
    public bool IsVisible { get; init; }
}
