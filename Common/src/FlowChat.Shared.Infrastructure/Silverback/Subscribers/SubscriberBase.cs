using FlowChat.Core.Exceptions;
using Microsoft.Extensions.Logging;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Subscribers;

namespace FlowChat.Shared.Infrastructure.Silverback.Subscribers;

public abstract class SubscriberBase<TIntegrationEvent>(ILogger logger)
{
    protected ILogger Logger { get; } = logger;

    [Subscribe]
    public async Task HandleAsync(
        IInboundEnvelope<TIntegrationEvent> envelope,
        CancellationToken cancellationToken)
    {
        var message = envelope.Message ?? throw new InvalidOperationException("Inbound envelope message cannot be null.");
        var sourceTopic = envelope.Endpoint.RawName;
        var deliveryKind = sourceTopic.EndsWith(".retry", StringComparison.OrdinalIgnoreCase)
            ? "retry"
            : "main";

        Logger.LogTrace(
            "Handling {EventType} in {SubscriberName} from {DeliveryKind} topic {SourceTopic}.",
            GetEventTypeName(message),
            GetType().Name,
            deliveryKind,
            sourceTopic);

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
