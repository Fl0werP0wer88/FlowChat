using FlowChat.Core.Messaging.AuthService.Events;
using FlowChat.NotificationService.Application.Features.Notification.Commands.UserEmailVerificationRequested;
using FlowChat.Shared.Infrastructure.Silverback.Subscribers;
using MediatR;

namespace FlowChat.NotificationService.Consumers.Kafka;

public sealed class UserEmailVerificationRequestedSubscriber(
    IMediator mediator,
    ILogger<UserEmailVerificationRequestedSubscriber> logger)
    : SubscriberBase<EmailVerificationRequestIntegrationEvent>(logger)
{
    protected override async Task ExecuteAsync(
        EmailVerificationRequestIntegrationEvent message,
        CancellationToken cancellationToken)
    {
        var userName = ResolveUserName(message.UserEmail);

        var result = await mediator.Send(
            new UserEmailVerificationRequestedCommand(
                message.UserId,
                message.UserEmail?.Trim() ?? string.Empty,
                userName ?? string.Empty,
                userName ?? string.Empty,
                message.ConfirmationLink?.Trim() ?? string.Empty,
                message.VerificationRequestId == Guid.Empty
                    ? message.UserId.ToString()
                    : message.VerificationRequestId.ToString("D")),
            cancellationToken);

        ThrowIfFailure(result);
    }

    private static string? ResolveUserName(string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return null;
        }

        var value = email.Trim();
        var atIndex = value.IndexOf('@');
        if (atIndex <= 0)
        {
            return null;
        }

        return value[..atIndex].Trim();
    }
}
