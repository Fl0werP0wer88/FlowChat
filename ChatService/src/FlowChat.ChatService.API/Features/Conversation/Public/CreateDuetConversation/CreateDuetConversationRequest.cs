using FlowChat.Core.Contracts;

namespace FlowChat.ChatService.Api.Features.Conversation.Public.CreateDuetConversation;

public sealed class CreateDuetConversationRequest : IServiceInput
{
    public Guid PartnerUserId { get; init; }
}
