using FlowChat.ChatService.Application.Features.Conversation.Dtos;

namespace FlowChat.ChatService.Application.Features.Conversation.Commands.CreateDuetConversation;

public sealed record CreateDuetConversationResult(
    DuetConversationDetailDto Conversation,
    bool WasCreated);
