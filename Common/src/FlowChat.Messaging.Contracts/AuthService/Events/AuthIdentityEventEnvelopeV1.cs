using System.Text.Json;

namespace FlowChat.Messaging.Contracts.AuthService.Events;

public sealed class AuthIdentityEventEnvelopeV1 : IntegrationEvent
{
    public required string EventType { get; init; }
    public int EventVersion { get; init; } = 1;
    public required JsonElement Payload { get; init; }
}
