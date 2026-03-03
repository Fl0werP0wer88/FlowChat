using FlowChat.Messaging.Contracts.AuthService.Events;
using FlowChat.SocialGraphService.Application.SocialGraphs.Commands.CreateDefaultSocialGraph;
using MediatR;
using Silverback.Messaging.Subscribers;

namespace FlowChat.SocialGraphService.Worker.Kafka;

public sealed class UserCreatedSubscriber(
    IMediator mediator,
    ILogger<UserCreatedSubscriber> logger)
{
    [Subscribe]
    public async Task HandleAsync(
        UserCreatedIntegrationEvent message,
        CancellationToken cancellationToken)
    {
        if (message.UserId == Guid.Empty)
        {
            throw new InvalidOperationException("Payload does not contain valid UserId.");
        }

        if (string.IsNullOrWhiteSpace(message.UserName))
        {
            throw new InvalidOperationException("Payload does not contain valid UserName.");
        }

        var result = await mediator.Send(
            new CreateDefaultSocialGraphCommand(
                message.UserId,
                message.UserName.Trim(),
                message.FirstName,
                message.LastName,
                message.Email?.Trim()),
            cancellationToken);

        if (result.IsSuccess)
        {
            logger.LogInformation(
                "Created default social graph for user {UserId}.",
                message.UserId);

            return;
        }

        if (result.Error.ErrorType.Name == "Conflict")
        {
            logger.LogInformation(
                "Skipping social graph creation for user {UserId}. Reason: {Reason}",
                message.UserId,
                result.Error.ErrorMessage);

            return;
        }

        if (result.Error.ErrorType.Name is "BadRequest" or "Validation")
        {
            throw new InvalidOperationException(result.Error.ErrorMessage);
        }

        throw new Exception(
            $"Failed to create default social graph for user '{message.UserId}': {result.Error.ErrorMessage}");
    }
}
