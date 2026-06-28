using FlowChat.AuthService.Application.Features.User.Commands.ConfirmAuthEmail;
using FlowChat.Core.Exceptions;
using FlowChat.Core.Messaging.UserProfileService.Events;
using FlowChat.Shared.Infrastructure.Silverback.Subscribers;
using MediatR;

namespace FlowChat.AuthService.Consumers.Kafka;

public sealed class UserEmailConfirmedSubscriber(
    IMediator mediator,
    ILogger<UserEmailConfirmedSubscriber> logger)
    : SubscriberBase<UserEmailConfirmedIntegrationEvent>(logger)
{
    protected override async Task ExecuteAsync(
        UserEmailConfirmedIntegrationEvent message,
        CancellationToken cancellationToken)
    {
        if (message.Email is not { Address: not null } email)
        {
            throw new NonTransientException("Payload does not contain Email.Address.");
        }

        var emailAddress = email.Address.Trim();
        if (string.IsNullOrWhiteSpace(emailAddress))
        {
            throw new NonTransientException("Payload does not contain Email.Address.");
        }

        if (!email.IsAuth)
        {
            Logger.LogDebug(
                "Skipping non-auth email confirmation for user profile {UserProfileId}, email {EmailId}.",
                message.UserProfileId,
                message.EmailId);
            return;
        }

        var result = await mediator.Send(
            new ConfirmAuthEmailCommand
            {
                EmailAddress = emailAddress
            },
            cancellationToken);

        ThrowIfFailure(result);
    }
}
