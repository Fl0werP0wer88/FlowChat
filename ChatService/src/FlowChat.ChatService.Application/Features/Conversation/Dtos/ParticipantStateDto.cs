namespace FlowChat.ChatService.Application.Features.Conversation.Dtos;

public sealed record ParticipantStateDto(
    Guid UserId,
    bool IsBlocked,
    bool IsHidden);
