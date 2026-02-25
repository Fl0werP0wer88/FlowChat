using MediatR;

namespace FlowChat.NotificationService.Application.Notifications.Commands;

public sealed record UserEmailVerificationRequestedCommand(
    Guid UserId,
    string Email,
    string UserName,
    string DisplayName,
    string ConfirmationLink,
    string? SourceMessageKey) : IRequest;
