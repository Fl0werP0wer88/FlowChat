using AutoFixture;
using FlowChat.Core.Domain;
using FlowChat.RealtimeService.Application.Contracts.Infrastructure;
using FlowChat.RealtimeService.Infrastructure.Routing;
using FlowChat.RealtimeService.Redis.Configuration.Settings;
using FlowChat.RealtimeService.Redis.RealtimeConnections;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Moq;

namespace FlowChat.RealtimeService.UnitTests;

public sealed class RealtimeEventRouterTests
{
    private readonly IFixture _fixture = new Fixture();
    private readonly Mock<IUserInstanceRoutingReader> _userInstanceRoutingReaderMock = new();
    private readonly Mock<IRealtimeInstanceAddressResolver> _addressResolverMock = new();
    private readonly Mock<IRealtimeInstanceInternalApiClient> _internalApiClientMock = new();
    private readonly Mock<IRealtimeClientDispatcher> _dispatcherMock = new();

    [Fact]
    public async Task RouteMessageAsync_WhenRecipientsAreLocalAndRemote_DispatchesLocalAndPostsRemote()
    {
        var localUser = _fixture.Create<Guid>();
        var remoteUser = _fixture.Create<Guid>();
        var notification = new ChatMessageParam(
            _fixture.Create<Guid>(),
            _fixture.Create<Guid>(),
            _fixture.Create<Guid>(),
            "Jane",
            "Hello",
            DateTimeOffset.UtcNow,
            [localUser, remoteUser]);
        IReadOnlyCollection<Guid>? localRecipients = null;
        List<(Uri BaseAddress, IReadOnlyCollection<Guid> Recipients)> remoteCalls = [];

        _userInstanceRoutingReaderMock
            .Setup(x => x.GetInstanceIdsByUserAsync(It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<Guid, IReadOnlyCollection<string>>
            {
                [localUser] = ["instance-local"],
                [remoteUser] = ["instance-remote"]
            });
        _addressResolverMock.Setup(x => x.Resolve("instance-remote")).Returns(new Uri("http://instance-remote"));
        _dispatcherMock
            .Setup(x => x.ReceiveMessageAsync(It.IsAny<ChatMessageParam>(), It.IsAny<CancellationToken>()))
            .Callback<ChatMessageParam, CancellationToken>((routedNotification, _) =>
                localRecipients = routedNotification.RecipientUserIds)
            .Returns(Task.CompletedTask);
        _internalApiClientMock
            .Setup(x => x.PublishMessageAsync(
                It.IsAny<Uri>(),
                It.IsAny<ChatMessageParam>(),
                It.IsAny<CancellationToken>()))
            .Callback<Uri, ChatMessageParam, CancellationToken>((baseAddress, routedNotification, _) =>
                remoteCalls.Add((baseAddress, routedNotification.RecipientUserIds)))
            .Returns(Task.CompletedTask);

        var router = CreateRouter();

        await router.RouteMessageAsync(notification, CancellationToken.None);

        localRecipients.Should().BeEquivalentTo([localUser]);
        remoteCalls.Should().ContainSingle();
        remoteCalls.Single().BaseAddress.Should().Be(new Uri("http://instance-remote"));
        remoteCalls.Single().Recipients.Should().BeEquivalentTo([remoteUser]);
    }

    [Fact]
    public async Task RoutePresenceChangeAsync_WhenRecipientsOffline_DoesNotDispatchOrCallInternalApi()
    {
        _userInstanceRoutingReaderMock
            .Setup(x => x.GetInstanceIdsByUserAsync(It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<Guid, IReadOnlyCollection<string>>());

        var router = CreateRouter();

        await router.RoutePresenceChangeAsync(
            new PresenceChangedParam(
                _fixture.Create<Guid>(),
                PresenceStatus.Active,
                DateTimeOffset.UtcNow,
                [_fixture.Create<Guid>()]),
            CancellationToken.None);

        _dispatcherMock.Verify(
            x => x.PresenceChangedAsync(It.IsAny<PresenceChangedParam>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _internalApiClientMock.Verify(
            x => x.PublishPresenceChangeAsync(
                It.IsAny<Uri>(),
                It.IsAny<PresenceChangedParam>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task RouteMessageAsync_WhenRemoteInstanceAddressMissing_ThrowsInvalidOperationException()
    {
        var userId = _fixture.Create<Guid>();
        _userInstanceRoutingReaderMock
            .Setup(x => x.GetInstanceIdsByUserAsync(It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<Guid, IReadOnlyCollection<string>>
            {
                [userId] = ["missing-instance"]
            });
        _addressResolverMock
            .Setup(x => x.Resolve("missing-instance"))
            .Throws(new InvalidOperationException("missing mapping"));

        var router = CreateRouter();

        var act = () => router.RouteMessageAsync(
            new ChatMessageParam(
                _fixture.Create<Guid>(),
                _fixture.Create<Guid>(),
                _fixture.Create<Guid>(),
                "Jane",
                "Hello",
                DateTimeOffset.UtcNow,
                [userId]),
            CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*missing mapping*");
    }

    private RealtimeEventRouter CreateRouter() =>
        new(
            _userInstanceRoutingReaderMock.Object,
            _addressResolverMock.Object,
            _internalApiClientMock.Object,
            _dispatcherMock.Object,
            Options.Create(new RealtimeConnectionsSettingsSection { InstanceId = "instance-local" }));
}
