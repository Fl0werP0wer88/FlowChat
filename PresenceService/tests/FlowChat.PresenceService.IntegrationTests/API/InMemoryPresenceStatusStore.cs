using FlowChat.Core.Domain;
using FlowChat.PresenceService.Application.Contracts.Infrastructure;
using FlowChat.PresenceService.Application.Features.Presence;

namespace FlowChat.PresenceService.IntegrationTests.API;

public sealed class InMemoryPresenceStatusStore : IPresenceStatusStore
{
    private readonly Dictionary<Guid, PresenceStatusSnapshot> _values = [];

    public Task<PresenceStatusSnapshot?> GetAsync(Guid userId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _values.TryGetValue(userId, out var value);
        return Task.FromResult(value);
    }

    public Task SetAsync(Guid userId, PresenceStatus status, DateTimeOffset changedAtUtc, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _values[userId] = new PresenceStatusSnapshot(userId, status, changedAtUtc);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Guid userId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _values.Remove(userId);
        return Task.CompletedTask;
    }
}
