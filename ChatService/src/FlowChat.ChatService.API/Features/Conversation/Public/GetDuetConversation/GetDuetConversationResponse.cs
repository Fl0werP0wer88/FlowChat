using FlowChat.Core.Contracts;

namespace FlowChat.ChatService.Api.Features.Conversation.Public.GetDuetConversation;

public sealed record GetDuetConversationResponse(
    Guid ConversationId,
    IReadOnlyCollection<ParticipantResponse> Participants) : IServiceOutput;

public sealed record ParticipantResponse(
    Guid UserId,
    string? DisplayName,
    string? AvatarUrl,
    Guid ParticipantUserId);
