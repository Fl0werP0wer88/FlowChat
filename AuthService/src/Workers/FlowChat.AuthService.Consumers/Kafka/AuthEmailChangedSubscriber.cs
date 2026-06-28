using FlowChat.AuthService.Application.Features.User.Commands.ChangeAuthEmail;
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
        var result = await mediator.Send(
            new ChangeAuthEmailCommand
            {
                UserId = message.UserProfileId,
                EmailAddress = message.EmailAddress?.Trim() ?? string.Empty
            },
            cancellationToken);

        ThrowIfFailure(result);
    }
}
