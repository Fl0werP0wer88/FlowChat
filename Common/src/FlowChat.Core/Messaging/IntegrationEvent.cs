namespace FlowChat.Core.Messaging;

public abstract class IntegrationEvent : IIntegrationEvent
{
    public string? Key { get; set; }
}
