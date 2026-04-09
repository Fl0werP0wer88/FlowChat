using FlowChat.Core.Contracts;

namespace FlowChat.Core.Messaging;

public abstract class IntegrationEvent : IConsumerInput
{
    public string? Key { get; set; }
}
