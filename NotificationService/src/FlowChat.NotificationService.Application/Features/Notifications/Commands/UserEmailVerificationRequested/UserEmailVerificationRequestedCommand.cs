using FlowChat.Shared.Application;
using MediatR;

namespace FlowChat.NotificationService.Application.Features.Notifications.Commands.UserEmailVerificationRequested;

public sealed record UserEmailVerificationRequestedCommand(
    Guid UserId,
    string Email,
    string UserName,
    string DisplayName,
    string ConfirmationLink,
    string? SourceMessageKey) : ICommand<Unit>;

