namespace FlowChat.ChatService.Application.Features.Conversation.Dtos;

public sealed record DuetConversationDetailDto(
    Guid ConversationId,
    IReadOnlyCollection<ConversationParticipantDto> Participants);
