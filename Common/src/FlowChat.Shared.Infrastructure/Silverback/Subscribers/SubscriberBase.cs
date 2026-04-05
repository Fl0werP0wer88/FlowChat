using FlowChat.Core.Exceptions;
using Microsoft.Extensions.Logging;
using Silverback.Messaging.Subscribers;

namespace FlowChat.Shared.Infrastructure.Silverback.Subscribers;

public abstract class SubscriberBase<TIntegrationEvent>(ILogger logger)
{
    protected ILogger Logger { get; } = logger;

    [Subscribe]
    public async Task HandleAsync(TIntegrationEvent message, CancellationToken cancellationToken)
    {
        try
        {
            await ExecuteAsync(message, cancellationToken);
        }
        catch (NonTransientException exception)
        {
            Logger.LogInformation(
                exception,
                "Skipping {EventType} in {SubscriberName}. Reason: {Reason}",
                GetEventTypeName(message),
                GetType().Name,
                exception.Message);

            throw;
        }
        catch (Exception exception)
        {
            Logger.LogWarning(
                exception,
                "Transient failure while handling {EventType} in {SubscriberName}.",
                GetEventTypeName(message),
                GetType().Name);

            throw;
        }
    }

    protected abstract Task ExecuteAsync(TIntegrationEvent message, CancellationToken cancellationToken);

    private static string GetEventTypeName(TIntegrationEvent message) => typeof(TIntegrationEvent).Name;
}
