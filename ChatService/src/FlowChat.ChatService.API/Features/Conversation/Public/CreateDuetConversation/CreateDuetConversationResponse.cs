using FlowChat.Core.Contracts;

namespace FlowChat.ChatService.Api.Features.Conversation.Public.CreateDuetConversation;

public sealed record CreateDuetConversationResponse(
    Guid ConversationId,
    IReadOnlyCollection<ParticipantResponse> Participants) : IServiceOutput;

public sealed record ParticipantResponse(
    Guid UserId,
    string? DisplayName,
    string? AvatarUrl,
    Guid ParticipantUserId);
