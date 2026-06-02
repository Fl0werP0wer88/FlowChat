namespace FlowChat.UserProfileService.Persistence.Entities;

public sealed class EmailReadEntity
{
    public Guid Id { get; init; }
    public Guid UserProfileId { get; init; }
    public string Address { get; init; } = string.Empty;
    public bool IsMain { get; init; }
    public bool IsAuth { get; init; }
    public bool IsConfirmed { get; init; }
    public bool IsVisible { get; init; }
}
