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
        var result = await mediator.Send(
            new CreateInitialUserProfileCommand(
                message.FriendlyUserId?.Trim() ?? string.Empty,
                message.Email?.Trim(),
                message.UserId,
                NormalizeOptional(message.FirstName),
                NormalizeOptional(message.LastName),
                NormalizeOptional(message.Organization)),
            cancellationToken);

        ThrowIfFailure(result);
    }

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
