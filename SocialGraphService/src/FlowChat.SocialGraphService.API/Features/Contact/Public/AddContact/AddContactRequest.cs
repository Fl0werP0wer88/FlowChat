namespace FlowChat.SocialGraphService.Api.Features.Contact.Public.AddContact;

public sealed class AddContactRequest
{
    public Guid OwnerUserId { get; set; }
    public Guid? UserId { get; set; }
    public string? FriendlyUserId { get; set; }
    public string? Email { get; set; }
}
