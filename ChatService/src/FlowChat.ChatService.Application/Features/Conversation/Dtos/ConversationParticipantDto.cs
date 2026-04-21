namespace FlowChat.ChatService.Application.Features.Conversation.Dtos;

public sealed record ConversationParticipantDto(
    Guid UserId,
    string? DisplayName,
    string? AvatarUrl,
    string FriendlyUserId);
