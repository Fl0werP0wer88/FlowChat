using FlowChat.Core.Contracts;
using FlowChat.Core.Domain;

namespace FlowChat.GatewayService.Api.Models;

public sealed record ContactWithConversationDto(
    Guid Id,
    Guid ContactUserId,
    string DisplayName,
    string? FirstName,
    string? LastName,
    string? PhoneNumber,
    string? Email,
    bool IsBlocked,
    Guid? ConversationId,
    PresenceStatus Status,
    DateTimeOffset PresenceChangedAtUtc) : IServiceOutput;
