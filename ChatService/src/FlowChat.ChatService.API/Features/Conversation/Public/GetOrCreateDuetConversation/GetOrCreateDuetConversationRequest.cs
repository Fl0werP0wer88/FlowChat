using FlowChat.Core.Contracts;

namespace FlowChat.ChatService.Api.Features.Conversation.Public.GetOrCreateDuetConversation;

public sealed class GetOrCreateDuetConversationRequest : IServiceInput
{
    public Guid RequestingUserId { get; init; }
    public Guid PartnerUserId { get; init; }
}
