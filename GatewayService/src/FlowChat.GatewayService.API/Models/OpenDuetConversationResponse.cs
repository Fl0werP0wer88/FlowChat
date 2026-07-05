using FlowChat.Core.Contracts;

namespace FlowChat.GatewayService.Api.Models;

public sealed record OpenDuetConversationResponse(
    Guid ConversationId,
    IReadOnlyCollection<ConversationParticipantResponse> Participants,
    IReadOnlyCollection<ConversationMessageResponse> Messages,
    DateTimeOffset? NextBeforeSentAtUtc,
    Guid? NextBeforeMessageId,
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
    string SenderDisplayName,
    string Text,
    DateTimeOffset SentAtUtc);
