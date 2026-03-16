namespace FlowChat.Messaging.Contracts;

public static class IntegrationMessageHeaders
{
    public const string EventId = "event-id";
    public const string OccurredOnUtc = "occurred-on-utc";
    public const string EventVersion = "event-version";
    public const string EventType = "event-type";
    public const string Source = "source";
    public const string CorrelationId = "correlation-id";
    public const string CausationId = "causation-id";
    public const string TraceId = "trace-id";
    public const string TraceParent = "traceparent";
}
