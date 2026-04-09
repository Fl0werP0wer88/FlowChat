using FlowChat.Core.Contracts;

namespace FlowChat.SocialGraphService.Api.Features.Contact.Public.AddContact;

public sealed record AddContactResponse(Guid ContactId) : IServiceOutput;
