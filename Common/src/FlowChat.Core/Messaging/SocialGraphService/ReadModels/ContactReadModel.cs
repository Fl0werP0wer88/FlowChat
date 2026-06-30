namespace FlowChat.Core.Messaging.SocialGraphService.ReadModels;

public sealed record ContactReadModel
{
    public Guid ContactId { get; init; }
    public Guid OwnerUserId { get; init; }
    public Guid ContactUserId { get; init; }
    public required string DisplayName { get; init; }
    public string? FirstName { get; init; }
    public string? LastName { get; init; }
    public string? PhoneNumber { get; init; }
    public string? EmailAddress { get; init; }
    public bool IsBlocked { get; init; }
}
