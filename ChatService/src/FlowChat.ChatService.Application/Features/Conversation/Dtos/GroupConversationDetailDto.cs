namespace FlowChat.ChatService.Application.Features.Conversation.Dtos;

public sealed record GroupConversationDetailDto(
    Guid ConversationId,
    string Name,
    IReadOnlyCollection<ConversationParticipantDto> Participants);
