using FlowChat.Core.Contracts;

namespace FlowChat.ChatService.Api.Features.Conversation.Public.GetDuetConversationIds;

public sealed record GetDuetConversationIdsResponse(
    IReadOnlyDictionary<Guid, Guid> ConversationIds) : IServiceOutput;
