using FlowChat.Core.Contracts;
using FlowChat.SocialGraphService.Application.Features.Contact.Queries.GetContactsForUser;

namespace FlowChat.SocialGraphService.Api.Features.Contact.Public.GetContactsForUser;

public sealed record GetContactsForUserResponse(IReadOnlyList<ContactDto> Contacts) : IServiceOutput;
