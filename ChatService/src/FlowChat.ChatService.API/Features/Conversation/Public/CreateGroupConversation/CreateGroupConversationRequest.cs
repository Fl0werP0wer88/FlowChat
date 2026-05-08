using FlowChat.Core.Contracts;

namespace FlowChat.ChatService.Api.Features.Conversation.Public.CreateGroupConversation;

public sealed class CreateGroupConversationRequest : IServiceInput
{
    public Guid ConversationId { get; init; }
    public IReadOnlyCollection<Guid> ParticipantUserIds { get; init; } = [];
    public string Name { get; init; } = string.Empty;
}
