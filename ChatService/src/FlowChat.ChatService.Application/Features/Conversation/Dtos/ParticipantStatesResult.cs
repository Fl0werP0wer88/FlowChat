namespace FlowChat.ChatService.Application.Features.Conversation.Dtos;

public sealed record ParticipantStatesResult(
    int ConversationVersion,
    IReadOnlyCollection<ParticipantStateDto> ParticipantStates);
