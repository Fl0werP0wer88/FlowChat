using FlowChat.Core.Exceptions;
using FlowChat.Core.Messaging.AuthService.Events;
using FlowChat.Shared.Infrastructure.Silverback.Subscribers;
using FlowChat.UserProfileService.Application.Features.UserProfile.Commands.CreateInitialUserProfile;
using MediatR;

namespace FlowChat.UserProfileService.Consumers.Kafka;

public sealed class AccountRegisteredSubscriber(
    IMediator mediator,
    ILogger<AccountRegisteredSubscriber> logger)
    : SubscriberBase<AccountRegisteredIntegrationEvent>(logger)
{
    protected override async Task ExecuteAsync(
        AccountRegisteredIntegrationEvent message,
        CancellationToken cancellationToken)
    {
        var friendlyUserId = message.FriendlyUserId?.Trim();
        if (string.IsNullOrWhiteSpace(friendlyUserId))
        {
            throw new NonTransientException("Payload does not contain FriendlyUserId.");
        }

        var userId = ResolveUserId(message.UserId);
        if (!userId.HasValue)
        {
            throw new NonTransientException("Payload does not contain valid UserId.");
        }

        var result = await mediator.Send(
            new CreateInitialUserProfileCommand(
                friendlyUserId,
                message.Email,
                userId.Value,
                NormalizeOptional(message.FirstName),
                NormalizeOptional(message.LastName),
                NormalizeOptional(message.Organization)),
            cancellationToken);

        ThrowIfFailure(result);
    }

    private static Guid? ResolveUserId(Guid payloadUserId) =>
        payloadUserId != Guid.Empty
            ? payloadUserId
            : null;

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
