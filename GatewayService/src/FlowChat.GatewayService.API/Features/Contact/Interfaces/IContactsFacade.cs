using FlowChat.Core.Results;
using FlowChat.GatewayService.Api.Features.Contact.Public.GetContactsWithConversations;

namespace FlowChat.GatewayService.Api.Features.Contact.Interfaces;

public interface IContactsFacade
{
    Task<FlowChatResult<GetContactsWithConversationsResult>> GetContactsWithConversationsAsync(
        CancellationToken cancellationToken);
}
