using AutoFixture;
using FlowChat.Core.Domain;
using FlowChat.PresenceService.Application.Contracts.Infrastructure;
using FlowChat.PresenceService.Application.Features.Presence;
using FlowChat.PresenceService.Application.Features.Presence.Queries.GetContactPresenceStatuses;
using FlowChat.PresenceService.Application.Features.Presence.Queries.GetPresenceStatusesBatch;
using FluentAssertions;
using Moq;

namespace FlowChat.PresenceService.UnitTests;

public sealed class GetPresenceStatusesBatchQueryHandlerTests
{
    private readonly IFixture _fixture = new Fixture();
    private readonly Mock<IPresenceStatusStore> _presenceStatusStoreMock = new();
    private readonly GetPresenceStatusesBatchQueryHandler _handler;

    public GetPresenceStatusesBatchQueryHandlerTests()
    {
        _presenceStatusStoreMock
            .Setup(x => x.GetManyAsync(It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<Guid, PresenceStatusSnapshot>());

        _handler = new GetPresenceStatusesBatchQueryHandler(_presenceStatusStoreMock.Object);
    }

    [Fact]
    public async Task Handle_WhenUserIdsAreEmpty_ReturnsEmptyResult()
    {
        var result = await _handler.Handle(
            new GetPresenceStatusesBatchQuery([]),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
        _presenceStatusStoreMock.Verify(
            x => x.GetManyAsync(It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WhenSnapshotsExist_ReturnsStoredStatuses()
    {
        var activeUserId = _fixture.Create<Guid>();
        var busyUserId = _fixture.Create<Guid>();
        var activeChangedAtUtc = new DateTimeOffset(2026, 5, 2, 10, 30, 0, TimeSpan.Zero);
        var busyChangedAtUtc = new DateTimeOffset(2026, 5, 2, 10, 45, 0, TimeSpan.Zero);

        _presenceStatusStoreMock
            .Setup(x => x.GetManyAsync(
                It.Is<IReadOnlyCollection<Guid>>(ids => ids.SequenceEqual(new[] { activeUserId, busyUserId })),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<Guid, PresenceStatusSnapshot>
            {
                [activeUserId] = new(activeUserId, PresenceStatus.Active, activeChangedAtUtc),
                [busyUserId] = new(busyUserId, PresenceStatus.Busy, busyChangedAtUtc)
            });

        var result = await _handler.Handle(
            new GetPresenceStatusesBatchQuery([activeUserId, busyUserId]),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEquivalentTo([
            new ContactPresenceStatusDto(activeUserId, PresenceStatus.Active, activeChangedAtUtc),
            new ContactPresenceStatusDto(busyUserId, PresenceStatus.Busy, busyChangedAtUtc)
        ]);
    }

    [Fact]
    public async Task Handle_WhenSnapshotIsMissing_ReturnsInvisibleStatus()
    {
        var storedUserId = _fixture.Create<Guid>();
        var missingUserId = _fixture.Create<Guid>();
        var changedAtUtc = new DateTimeOffset(2026, 5, 2, 11, 0, 0, TimeSpan.Zero);

        _presenceStatusStoreMock
            .Setup(x => x.GetManyAsync(It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<Guid, PresenceStatusSnapshot>
            {
                [storedUserId] = new(storedUserId, PresenceStatus.AFK, changedAtUtc)
            });

        var result = await _handler.Handle(
            new GetPresenceStatusesBatchQuery([storedUserId, missingUserId]),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEquivalentTo([
            new ContactPresenceStatusDto(storedUserId, PresenceStatus.AFK, changedAtUtc),
            new ContactPresenceStatusDto(missingUserId, PresenceStatus.Invisible, DateTimeOffset.MinValue)
        ]);
    }
}
