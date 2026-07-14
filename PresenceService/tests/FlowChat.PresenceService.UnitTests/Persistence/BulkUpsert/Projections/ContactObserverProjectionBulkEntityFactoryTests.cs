using FlowChat.PresenceService.Application.Features.ContactObserverProjections;
using FlowChat.PresenceService.Persistence.BulkUpsert.Projections;
using FlowChat.PresenceService.Persistence.Entities;
using FlowChat.Core.Messaging;
using FlowChat.Shared.Application;
using FluentAssertions;

namespace FlowChat.PresenceService.UnitTests.Persistence.BulkUpsert.Projections;

public sealed class ContactObserverProjectionBulkEntityFactoryTests
{
    private readonly ContactObserverProjectionBulkEntityFactory _factory = new();

    [Fact]
    public void UpdateByProperties_ReturnsObservedAndObserverUserIdColumns()
    {
        _factory.UpdateByProperties.Should().BeEquivalentTo(
        [
            nameof(ContactObserverReadModelEntity.ObservedUserId),
            nameof(ContactObserverReadModelEntity.ObserverUserId)
        ]);
    }

    [Fact]
    public void CreateUpsertEntity_MapsAllFields()
    {
        var observedUserId = Guid.NewGuid();
        var observerUserId = Guid.NewGuid();
        var createdAt = DateTimeOffset.UtcNow.AddMinutes(-5);
        var modifiedAt = DateTimeOffset.UtcNow;

        var entity = _factory.CreateUpsertEntity(
            new ContactObserverProjectionDto
            {
                ObservedUserId = observedUserId,
                ObserverUserId = observerUserId,
                IsBlocked = true,
                Source = "chat-duet-conversation-events"
            },
            sourceVersion: 3,
            sourceCreatedAtUtc: createdAt,
            sourceLastModifiedAtUtc: modifiedAt,
            sourceDeletedAtUtc: null);

        entity.ObservedUserId.Should().Be(observedUserId);
        entity.ObserverUserId.Should().Be(observerUserId);
        entity.IsBlocked.Should().BeTrue();
        entity.SourceVersion.Should().Be(3);
        entity.SourceCreatedAtUtc.Should().Be(createdAt);
        entity.SourceLastModifiedAtUtc.Should().Be(modifiedAt);
        entity.SourceDeletedAtUtc.Should().BeNull();
    }

    [Fact]
    public void CreateTombstoneEntity_WhenSourceDeletedAtUtcIsNull_FallsBackToNow()
    {
        var observedUserId = Guid.NewGuid();
        var observerUserId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        var item = new ProjectionCommandItem<ContactObserverProjectionDto>(
            new ContactObserverProjectionDto
            {
                ObservedUserId = observedUserId,
                ObserverUserId = observerUserId
            },
            OperationType.Deleted,
            SourceVersion: 4,
            SourceCreatedAtUtc: now.AddMinutes(-10),
            SourceLastModifiedAtUtc: now.AddMinutes(-1),
            SourceDeletedAtUtc: null);

        var entity = _factory.CreateTombstoneEntity(item, now);

        entity.ObservedUserId.Should().Be(observedUserId);
        entity.ObserverUserId.Should().Be(observerUserId);
        entity.SourceVersion.Should().Be(4);
        entity.SourceDeletedAtUtc.Should().Be(now);
    }
}
