using AutoFixture;
using FlowChat.Core.Domain;
using FlowChat.RealtimeService.Application.Contracts.Infrastructure;
using FlowChat.RealtimeService.Infrastructure.Routing;
using FlowChat.RealtimeService.Redis.RealtimeConnections;
using FluentAssertions;
using Moq;

namespace FlowChat.RealtimeService.UnitTests;

public sealed class WorkerRealtimeEventRouterTests
{
    private readonly IFixture _fixture = new Fixture();
    private readonly Mock<IUserInstanceRoutingReader> _userInstanceRoutingReaderMock = new();
    private readonly Mock<IRealtimeInstanceAddressResolver> _addressResolverMock = new();
    private readonly Mock<IRealtimeInstanceInternalApiClient> _internalApiClientMock = new();

    [Fact]
    public async Task RouteMessageAsync_GroupsRecipientsByInstanceAndPublishesToEveryInstance()
    {
        var firstUser = _fixture.Create<Guid>();
        var secondUser = _fixture.Create<Guid>();
        var thirdUser = _fixture.Create<Guid>();
        List<(Uri BaseAddress, IReadOnlyCollection<Guid> Recipients)> calls = [];

        _userInstanceRoutingReaderMock
            .Setup(x => x.GetInstanceIdsByUserAsync(It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<Guid, IReadOnlyCollection<string>>
            {
                [firstUser] = ["instance-a"],
                [secondUser] = ["instance-a", "instance-b"],
                [thirdUser] = []
            });
        _addressResolverMock.Setup(x => x.Resolve("instance-a")).Returns(new Uri("http://instance-a"));
        _addressResolverMock.Setup(x => x.Resolve("instance-b")).Returns(new Uri("http://instance-b"));
        _internalApiClientMock
            .Setup(x => x.PublishMessageAsync(
                It.IsAny<Uri>(),
                It.IsAny<ChatMessageParam>(),
                It.IsAny<CancellationToken>()))
            .Callback<Uri, ChatMessageParam, CancellationToken>((baseAddress, notification, _) =>
                calls.Add((baseAddress, notification.RecipientUserIds)))
            .Returns(Task.CompletedTask);

        var router = CreateRouter();

        await router.RouteMessageAsync(
            new ChatMessageParam(
                _fixture.Create<Guid>(),
                _fixture.Create<Guid>(),
                _fixture.Create<Guid>(),
                "Jane",
                "Hello",
                DateTimeOffset.UtcNow,
                DateTimeOffset.UtcNow,
                [firstUser, secondUser, thirdUser, secondUser, Guid.Empty]),
            CancellationToken.None);

        calls.Should().HaveCount(2);
        calls.Should().Contain(call =>
            call.BaseAddress == new Uri("http://instance-a")
            && call.Recipients.OrderBy(x => x).SequenceEqual(new[] { firstUser, secondUser }.OrderBy(x => x)));
        var instanceBCall = calls.Should().ContainSingle(call => call.BaseAddress == new Uri("http://instance-b")).Subject;
        instanceBCall.Recipients.Should().ContainSingle().Which.Should().Be(secondUser);
    }

    [Fact]
    public async Task RoutePresenceChangeAsync_WhenRecipientsHaveNoActiveInstance_DoesNotPublish()
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

        _internalApiClientMock.Verify(
            x => x.PublishPresenceChangeAsync(
                It.IsAny<Uri>(),
                It.IsAny<PresenceChangedParam>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task RoutePresenceChangeAsync_PublishesThroughInstanceInternalApiClient()
    {
        var userId = _fixture.Create<Guid>();
        Uri? calledBaseAddress = null;
        IReadOnlyCollection<Guid>? calledRecipients = null;

        _userInstanceRoutingReaderMock
            .Setup(x => x.GetInstanceIdsByUserAsync(It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<Guid, IReadOnlyCollection<string>>
            {
                [userId] = ["instance-a"]
            });
        _addressResolverMock.Setup(x => x.Resolve("instance-a")).Returns(new Uri("http://instance-a"));
        _internalApiClientMock
            .Setup(x => x.PublishPresenceChangeAsync(
                It.IsAny<Uri>(),
                It.IsAny<PresenceChangedParam>(),
                It.IsAny<CancellationToken>()))
            .Callback<Uri, PresenceChangedParam, CancellationToken>((baseAddress, notification, _) =>
            {
                calledBaseAddress = baseAddress;
                calledRecipients = notification.RecipientUserIds;
            })
            .Returns(Task.CompletedTask);

        var router = CreateRouter();

        await router.RoutePresenceChangeAsync(
            new PresenceChangedParam(
                _fixture.Create<Guid>(),
                PresenceStatus.Active,
                DateTimeOffset.UtcNow,
                [userId]),
            CancellationToken.None);

        calledBaseAddress.Should().Be(new Uri("http://instance-a"));
        calledRecipients.Should().ContainSingle().Which.Should().Be(userId);
    }

    [Fact]
    public async Task RouteConversationChangedAsync_PublishesThroughInstanceInternalApiClient()
    {
        var userId = _fixture.Create<Guid>();
        Uri? calledBaseAddress = null;
        IReadOnlyCollection<Guid>? calledParticipants = null;

        _userInstanceRoutingReaderMock
            .Setup(x => x.GetInstanceIdsByUserAsync(It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<Guid, IReadOnlyCollection<string>>
            {
                [userId] = ["instance-a"]
            });
        _addressResolverMock.Setup(x => x.Resolve("instance-a")).Returns(new Uri("http://instance-a"));
        _internalApiClientMock
            .Setup(x => x.PublishConversationChangedAsync(
                It.IsAny<Uri>(),
                It.IsAny<ConversationChangedParam>(),
                It.IsAny<CancellationToken>()))
            .Callback<Uri, ConversationChangedParam, CancellationToken>((baseAddress, notification, _) =>
            {
                calledBaseAddress = baseAddress;
                calledParticipants = notification.ParticipantUserIds;
            })
            .Returns(Task.CompletedTask);

        var router = CreateRouter();

        await router.RouteConversationChangedAsync(
            new ConversationChangedParam(
                _fixture.Create<Guid>(),
                2,
                "Dev Team",
                _fixture.Create<Guid>(),
                [userId]),
            CancellationToken.None);

        calledBaseAddress.Should().Be(new Uri("http://instance-a"));
        calledParticipants.Should().ContainSingle().Which.Should().Be(userId);
    }

    private WorkerRealtimeEventRouter CreateRouter() =>
        new(
            _userInstanceRoutingReaderMock.Object,
            _addressResolverMock.Object,
            _internalApiClientMock.Object);
}
