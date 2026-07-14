namespace FlowChat.Core.Messaging.UserProfileService.Events;

public sealed record UserProfilePhone
{
    public required string Number { get; init; }
    public bool IsConfirmed { get; init; }
    public bool IsVisible { get; init; }
}
