using FlowChat.Core.Messaging;
using FlowChat.Shared.Application;

namespace FlowChat.UserProfileService.IntegrationTests.API;

public sealed class RecordingIntegrationEventPublisher : IIntegrationEventPublisher
{
    private readonly List<IntegrationEvent> _published = [];

    public IReadOnlyList<IntegrationEvent> Published => _published.AsReadOnly();

    public Task PublishToOutboxAsync<TEvent>(TEvent message, CancellationToken cancellationToken)
        where TEvent : IntegrationEvent
    {
        _published.Add(message);
        return Task.CompletedTask;
    }

    public IReadOnlyList<TEvent> PublishedOfType<TEvent>() where TEvent : IntegrationEvent =>
        _published.OfType<TEvent>().ToList().AsReadOnly();

    public void Clear() => _published.Clear();
}
