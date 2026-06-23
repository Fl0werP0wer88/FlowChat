using FlowChat.Core.Contracts;

namespace FlowChat.GatewayService.Api.Models;

public sealed record GetContactsWithConversationsResponse(
    IReadOnlyList<ContactWithConversationDto> Contacts) : IServiceOutput;
