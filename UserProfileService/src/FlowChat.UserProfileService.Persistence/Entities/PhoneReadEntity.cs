namespace FlowChat.UserProfileService.Persistence.Entities;

public sealed class PhoneReadEntity
{
    public Guid Id { get; init; }
    public Guid UserProfileId { get; init; }
    public string Number { get; init; } = string.Empty;
    public bool IsMain { get; init; }
    public bool IsConfirmed { get; init; }
    public bool IsVisible { get; init; }
}
