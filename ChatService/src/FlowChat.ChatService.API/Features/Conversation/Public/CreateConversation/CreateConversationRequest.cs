using FlowChat.Core.Contracts;

namespace FlowChat.ChatService.Api.Features.Conversation.Public.CreateConversation;

public sealed class CreateConversationRequest : IServiceInput
{
    public bool IsGroup { get; init; }
    public Guid CreatedByUserId { get; init; }
    public IReadOnlyCollection<Guid> ParticipantUserIds { get; init; } = [];
    public string? Name { get; init; }
}
