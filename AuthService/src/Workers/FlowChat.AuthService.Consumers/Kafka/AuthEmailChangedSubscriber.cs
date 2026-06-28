using FlowChat.AuthService.Application.Features.User.Commands.ChangeAuthEmail;
using FlowChat.Core.Exceptions;
using FlowChat.Core.Messaging.UserProfileService.Events;
using FlowChat.Shared.Infrastructure.Silverback.Subscribers;
using MediatR;

namespace FlowChat.AuthService.Consumers.Kafka;

public sealed class AuthEmailChangedSubscriber(
    IMediator mediator,
    ILogger<AuthEmailChangedSubscriber> logger)
    : SubscriberBase<AuthEmailChangedIntegrationEvent>(logger)
{
    protected override async Task ExecuteAsync(
        AuthEmailChangedIntegrationEvent message,
        CancellationToken cancellationToken)
    {
        if (message.UserProfileId == Guid.Empty)
        {
            throw new NonTransientException("Payload does not contain valid UserProfileId.");
        }

        var emailAddress = message.EmailAddress?.Trim();
        if (string.IsNullOrWhiteSpace(emailAddress))
        {
            throw new NonTransientException("Payload does not contain EmailAddress.");
        }

        var result = await mediator.Send(
            new ChangeAuthEmailCommand
            {
                UserId = message.UserProfileId,
                EmailAddress = emailAddress
            },
            cancellationToken);

        ThrowIfFailure(result);
    }
}
