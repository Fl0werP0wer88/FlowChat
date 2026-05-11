using FlowChat.Core.Contracts;

namespace FlowChat.GatewayService.Api.Models;

public sealed record OpenDuetConversationResponse(
    Guid ConversationId,
    IReadOnlyCollection<ConversationParticipantDto> Participants,
    IReadOnlyCollection<ConversationMessageDto> Messages,
    DateTimeOffset? NextBeforeSentAtUtc,
    Guid? NextBeforeMessageId,
    bool HasMore) : IServiceOutput;

public sealed record ConversationParticipantDto(
    Guid UserId,
    string? DisplayName,
    string? AvatarUrl,
    Guid ParticipantUserId) : IServiceOutput;

public sealed record ConversationMessageDto(
    Guid Id,
    Guid ConversationId,
    Guid SenderUserId,
    string SenderDisplayName,
    string Text,
    DateTimeOffset SentAtUtc) : IServiceOutput;
