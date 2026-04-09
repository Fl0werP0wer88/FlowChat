namespace FlowChat.Core.Messaging.UserProfileService.Events;

public sealed class UserProfilePhone
{
    public Guid Id { get; init; }
    public Guid UserProfileId { get; init; }
    public required string Number { get; init; }
    public bool IsMain { get; init; }
    public bool IsConfirmed { get; init; }
    public bool IsVisible { get; init; }
}
