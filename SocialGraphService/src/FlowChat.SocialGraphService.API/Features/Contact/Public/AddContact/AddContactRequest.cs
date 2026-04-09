using FlowChat.Core.Contracts;

namespace FlowChat.SocialGraphService.Api.Features.Contact.Public.AddContact;

public sealed class AddContactRequest : IServiceInput
{
    public Guid OwnerUserId { get; set; }
    public Guid? UserId { get; set; }
    public string? FriendlyUserId { get; set; }
    public string? Email { get; set; }
}
