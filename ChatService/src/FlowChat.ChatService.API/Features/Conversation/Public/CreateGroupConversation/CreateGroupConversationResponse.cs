using FlowChat.Core.Contracts;

namespace FlowChat.ChatService.Api.Features.Conversation.Public.CreateGroupConversation;

public sealed record CreateGroupConversationResponse(
    Guid ConversationId,
    string Name,
    IReadOnlyCollection<ParticipantResponse> Participants) : IServiceOutput;

public sealed record ParticipantResponse(
    Guid UserId,
    string? DisplayName,
    string? AvatarUrl,
    Guid ParticipantUserId);
