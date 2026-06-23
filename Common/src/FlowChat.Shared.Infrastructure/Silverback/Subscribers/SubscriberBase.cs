using System.Diagnostics;
using FlowChat.Core.Exceptions;
using FlowChat.Core.Messaging;
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

        Activity.Current?.SetTag("flowchat.subscriber.event_type", GetEventTypeName(message));
        Activity.Current?.SetTag("flowchat.subscriber.name", GetType().Name);
        Activity.Current?.SetTag("flowchat.subscriber.delivery_kind", deliveryKind);
        Activity.Current?.SetTag("flowchat.subscriber.source_topic", sourceTopic);
        Activity.Current?.SetTag("flowchat.subscriber.message_id", envelope.Headers.GetValue(IntegrationMessageHeaders.EventId));

        try
        {
            await ExecuteAsync(message, cancellationToken);
            Activity.Current?.SetTag("flowchat.subscriber.result", "success");
        }
        catch (NonTransientException exception)
        {
            Activity.Current?.SetTag("flowchat.subscriber.result", "non_transient_failure");
            Logger.LogInformation(
                exception,
                "Skipping {EventType} in {SubscriberName}. Reason: {Reason}",
                GetEventTypeName(message),
                GetType().Name,
                exception.Message);

            throw;
        }
        catch (TransientException exception)
        {
            Activity.Current?.SetTag("flowchat.subscriber.result", "transient_failure");
            Logger.LogWarning(
                exception,
                "Transient failure while handling {EventType} in {SubscriberName}.",
                GetEventTypeName(message),
                GetType().Name);

            throw;
        }
        catch (Exception exception)
        {
            Activity.Current?.SetTag("flowchat.subscriber.result", "unknown_failure");
            Logger.LogError(
                exception,
                "Unexpected failure while handling {EventType} in {SubscriberName}.",
                GetEventTypeName(message),
                GetType().Name);

            throw;
        }
    }

    protected abstract Task ExecuteAsync(TIntegrationEvent message, CancellationToken cancellationToken);

    private static string GetEventTypeName(TIntegrationEvent message) => typeof(TIntegrationEvent).Name;
}
