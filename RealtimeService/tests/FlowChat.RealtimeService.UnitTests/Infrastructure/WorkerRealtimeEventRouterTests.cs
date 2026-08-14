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
    public async Task RouteMessageAsync_BroadcastsUnfilteredNotificationToEveryTargetInstance()
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
        var originalRecipients = new[] { firstUser, secondUser, thirdUser, secondUser, Guid.Empty };

        await router.RouteMessageAsync(
            new ChatMessageParam(
                _fixture.Create<Guid>(),
                _fixture.Create<Guid>(),
                _fixture.Create<Guid>(),
                "Hello",
                42,
                DateTimeOffset.UtcNow,
                DateTimeOffset.UtcNow,
                originalRecipients),
            CancellationToken.None);

        calls.Should().HaveCount(2);
        calls.Select(call => call.BaseAddress).Should().BeEquivalentTo([new Uri("http://instance-a"), new Uri("http://instance-b")]);
        calls.Should().OnlyContain(call => call.Recipients.SequenceEqual(originalRecipients));
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
    public async Task RouteGroupConversationChangedAsync_PublishesThroughInstanceInternalApiClient()
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
            .Setup(x => x.PublishGroupConversationChangedAsync(
                It.IsAny<Uri>(),
                It.IsAny<GroupConversationChangedParam>(),
                It.IsAny<CancellationToken>()))
            .Callback<Uri, GroupConversationChangedParam, CancellationToken>((baseAddress, notification, _) =>
            {
                calledBaseAddress = baseAddress;
                calledParticipants = notification.ParticipantUserIds;
            })
            .Returns(Task.CompletedTask);

        var router = CreateRouter();

        await router.RouteGroupConversationChangedAsync(
            new GroupConversationChangedParam(
                _fixture.Create<Guid>(),
                2,
                "Dev Team",
                _fixture.Create<Guid>(),
                [userId]),
            CancellationToken.None);

        calledBaseAddress.Should().Be(new Uri("http://instance-a"));
        calledParticipants.Should().ContainSingle().Which.Should().Be(userId);
    }

    [Fact]
    public async Task RouteConversationParticipantsAddedAsync_SelectsInstancesByRecipientUserIds()
    {
        var changedUserId = _fixture.Create<Guid>();
        var existingUserId = _fixture.Create<Guid>();
        ConversationParticipantsAddedParam? publishedParam = null;
        IReadOnlyCollection<Guid>? routingUserIds = null;

        _userInstanceRoutingReaderMock
            .Setup(x => x.GetInstanceIdsByUserAsync(It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()))
            .Callback<IReadOnlyCollection<Guid>, CancellationToken>((userIds, _) => routingUserIds = userIds)
            .ReturnsAsync(new Dictionary<Guid, IReadOnlyCollection<string>>
            {
                [existingUserId] = ["instance-a"]
            });
        _addressResolverMock.Setup(x => x.Resolve("instance-a")).Returns(new Uri("http://instance-a"));
        _internalApiClientMock
            .Setup(x => x.PublishConversationParticipantsAddedAsync(
                It.IsAny<Uri>(),
                It.IsAny<ConversationParticipantsAddedParam>(),
                It.IsAny<CancellationToken>()))
            .Callback<Uri, ConversationParticipantsAddedParam, CancellationToken>((_, param, _) => publishedParam = param)
            .Returns(Task.CompletedTask);

        await CreateRouter().RouteConversationParticipantsAddedAsync(
            new ConversationParticipantsAddedParam(
                _fixture.Create<Guid>(),
                2,
                [changedUserId],
                [changedUserId, existingUserId]),
            CancellationToken.None);

        routingUserIds.Should().BeEquivalentTo([changedUserId, existingUserId]);
        publishedParam.Should().NotBeNull();
        publishedParam!.ParticipantUserIds.Should().ContainSingle().Which.Should().Be(changedUserId);
    }

    [Fact]
    public async Task RouteConversationParticipantsRemovedAsync_SelectsInstancesByRecipientUserIds()
    {
        var removedUserId = _fixture.Create<Guid>();
        var remainingUserId = _fixture.Create<Guid>();
        IReadOnlyCollection<Guid>? routingUserIds = null;

        _userInstanceRoutingReaderMock
            .Setup(x => x.GetInstanceIdsByUserAsync(It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()))
            .Callback<IReadOnlyCollection<Guid>, CancellationToken>((userIds, _) => routingUserIds = userIds)
            .ReturnsAsync(new Dictionary<Guid, IReadOnlyCollection<string>>());

        await CreateRouter().RouteConversationParticipantsRemovedAsync(
            new ConversationParticipantsRemovedParam(
                _fixture.Create<Guid>(),
                1,
                [removedUserId],
                [removedUserId, remainingUserId]),
            CancellationToken.None);

        routingUserIds.Should().BeEquivalentTo([removedUserId, remainingUserId]);
    }

    private WorkerRealtimeEventRouter CreateRouter() =>
        new(
            _userInstanceRoutingReaderMock.Object,
            _addressResolverMock.Object,
            _internalApiClientMock.Object);
}
