using FlowChat.Core.Contracts;

namespace FlowChat.SocialGraphService.Api.Features.Contact.Public.GetContactsForUser;

public sealed class GetContactsForUserRequest : IServiceInput
{
    public Guid UserId { get; set; }
}
