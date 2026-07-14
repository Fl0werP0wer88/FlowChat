using FlowChat.Core.Contracts;

namespace FlowChat.GatewayService.Api.Models;

public sealed record OpenGroupConversationResponse(
    Guid ConversationId,
    string Name,
    IReadOnlyCollection<ConversationParticipantResponse> Participants,
    IReadOnlyCollection<ConversationMessageResponse> Messages,
    DateTimeOffset? NextBeforeSentAtUtc,
    Guid? NextBeforeMessageId,
    bool HasMore) : IServiceOutput;
