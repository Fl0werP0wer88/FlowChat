using System.Diagnostics;
using FlowChat.Core.Exceptions;
using FlowChat.Core.Messaging;
using FlowChat.Core.Results;
using FlowChat.Shared.Domain;
using FlowChat.Shared.Infrastructure.Silverback.Kafka.Retry;
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
        var headers = envelope.Headers;
        var retryAttempt = headers?.GetValue(RetryMessageHeaders.RetryAttempt);
        var deliveryKind = string.IsNullOrWhiteSpace(retryAttempt)
            ? "main"
            : "retry";

        Activity.Current?.SetTag("flowchat.subscriber.event_type", GetEventTypeName(message));
        Activity.Current?.SetTag("flowchat.subscriber.name", GetType().Name);
        Activity.Current?.SetTag("flowchat.subscriber.delivery_kind", deliveryKind);
        Activity.Current?.SetTag("flowchat.subscriber.source_topic", sourceTopic);
        Activity.Current?.SetTag("flowchat.subscriber.message_id", headers?.GetValue(IntegrationMessageHeaders.EventId));

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
        catch (IsolableException exception)
        {
            Activity.Current?.SetTag("flowchat.subscriber.result", "isolable_failure");
            Logger.LogInformation(
                exception,
                "Isolable failure while handling {EventType} in {SubscriberName}. Reason: {Reason}",
                GetEventTypeName(message),
                GetType().Name,
                exception.Message);

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

    protected static void ThrowIfFailure<TValue>(FlowChatResult<TValue> result)
    {
        if (result.IsSuccess)
        {
            return;
        }

        var message = result.Error.ErrorMessage ?? "Command failed.";

        throw result.Error.FailureKind switch
        {
            FailureKind.Transient => new TransientException(message),
            FailureKind.Isolable => new IsolableException(message),
            _ => new NonTransientException(message)
        };
    }

    private static string GetEventTypeName(TIntegrationEvent message) => typeof(TIntegrationEvent).Name;
}
