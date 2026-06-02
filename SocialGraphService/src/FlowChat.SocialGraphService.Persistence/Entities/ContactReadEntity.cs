namespace FlowChat.SocialGraphService.Persistence.Entities;

public sealed class ContactReadEntity
{
    public Guid Id { get; init; }
    public Guid OwnerUserId { get; init; }
    public Guid ContactUserId { get; init; }
    public string DisplayName { get; init; } = string.Empty;
    public string? FirstName { get; init; }
    public string? LastName { get; init; }
    public string? PhoneNumber { get; init; }
    public string? EmailAddress { get; init; }
    public bool IsBlocked { get; init; }
    public DateTimeOffset CreatedAtUtc { get; init; }
}
