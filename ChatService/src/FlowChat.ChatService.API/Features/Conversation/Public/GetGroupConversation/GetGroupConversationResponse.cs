using FlowChat.Core.Contracts;

namespace FlowChat.ChatService.Api.Features.Conversation.Public.GetGroupConversation;

public sealed record GetGroupConversationResponse(
    Guid ConversationId,
    string Name,
    IReadOnlyCollection<ParticipantResponse> Participants) : IServiceOutput;

public sealed record ParticipantResponse(
    Guid UserId,
    string? DisplayName,
    string? AvatarUrl,
    Guid ParticipantUserId);
