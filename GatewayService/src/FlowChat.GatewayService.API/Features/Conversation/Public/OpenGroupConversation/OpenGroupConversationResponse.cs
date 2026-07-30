using FlowChat.Core.Contracts;

namespace FlowChat.GatewayService.Api.Features.Conversation.Public.OpenGroupConversation;

public sealed record OpenGroupConversationResponse(
    Guid ConversationId,
    string Name,
    IReadOnlyCollection<ConversationParticipantResponse> Participants,
    IReadOnlyCollection<ConversationMessageResponse> Messages,
    long? NextBeforeSequenceNum,
    long CurrentSequenceNum,
    bool HasMore) : IServiceOutput;

public sealed record ConversationParticipantResponse(
    Guid UserId,
    string? DisplayName,
    string? AvatarUrl,
    Guid ParticipantUserId);

public sealed record ConversationMessageResponse(
    Guid Id,
    Guid ConversationId,
    Guid SenderUserId,
    string Text,
    DateTimeOffset SentAtUtc,
    long SequenceNum);
