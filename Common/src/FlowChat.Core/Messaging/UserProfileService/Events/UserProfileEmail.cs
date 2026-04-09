namespace FlowChat.Core.Messaging.UserProfileService.Events;

public sealed class UserProfileEmail
{
    public Guid Id { get; init; }
    public Guid UserProfileId { get; init; }
    public required string Address { get; init; }
    public bool IsMain { get; init; }
    public bool IsAuth { get; init; }
    public bool IsConfirmed { get; init; }
    public bool IsVisible { get; init; }
}
