using FlowChat.Core.Contracts;
using FlowChat.NotificationService.Domain.Enums;

namespace FlowChat.NotificationService.Application.Features.Notification.Queries.GetNotifications;

public sealed record NotificationDto(
    Guid Id,
    Guid UserId,
    string Email,
    string DisplayName,
    string Body,
    NotificationType Type,
    NotificationStatus Status,
    string? ProviderMessageId,
    string? FailureReason,
    string? SourceMessageKey,
    DateTimeOffset? SentAtUtc,
    DateTimeOffset CreatedDate) : IDbResponse;
