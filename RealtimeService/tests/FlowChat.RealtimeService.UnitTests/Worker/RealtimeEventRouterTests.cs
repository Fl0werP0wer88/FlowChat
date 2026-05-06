using AutoFixture;
using FlowChat.Core.Domain;
using FlowChat.RealtimeService.Consumers.Realtime.Contracts;
using FlowChat.RealtimeService.Consumers.Services;
using FlowChat.RealtimeService.Redis.Routing;
using FluentAssertions;
using Moq;

namespace FlowChat.RealtimeService.UnitTests;

public sealed class RealtimeEventRouterTests
{
    private readonly IFixture _fixture = new Fixture();
    private readonly Mock<IUserInstanceRoutingReader> _userInstanceRoutingReaderMock = new();
    private readonly Mock<IRealtimeInstanceAddressResolver> _addressResolverMock = new();
    private readonly Mock<IRealtimeInternalApiClient> _internalApiClientMock = new();

    [Fact]
    public async Task PublishMessageAsync_WhenRecipientsShareInstances_SendsOneRequestPerInstance()
    {
        var userA = _fixture.Create<Guid>();
        var userB = _fixture.Create<Guid>();
        var request = new PublishMessageRequest
        {
            MessageId = _fixture.Create<Guid>(),
            ConversationId = _fixture.Create<Guid>(),
            SenderUserId = _fixture.Create<Guid>(),
            SenderDisplayName = "Jane",
            Text = "Hello",
            SentAtUtc = DateTimeOffset.UtcNow,
            RecipientUserIds = [userA, userB]
        };
        List<(Uri BaseAddress, PublishMessageRequest Request)> calls = [];

        _userInstanceRoutingReaderMock
            .Setup(x => x.GetInstanceIdsByUserAsync(It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<Guid, IReadOnlyCollection<string>>
            {
                [userA] = ["instance-a", "instance-b"],
                [userB] = ["instance-b"]
            });
        _addressResolverMock.Setup(x => x.Resolve("instance-a")).Returns(new Uri("http://instance-a"));
        _addressResolverMock.Setup(x => x.Resolve("instance-b")).Returns(new Uri("http://instance-b"));
        _internalApiClientMock
            .Setup(x => x.PublishMessageAsync(It.IsAny<Uri>(), It.IsAny<PublishMessageRequest>(), It.IsAny<CancellationToken>()))
            .Callback<Uri, PublishMessageRequest, CancellationToken>((baseAddress, routedRequest, _) => calls.Add((baseAddress, routedRequest)))
            .Returns(Task.CompletedTask);

        var router = CreateRouter();

        await router.PublishMessageAsync(request, CancellationToken.None);

        calls.Should().HaveCount(2);
        var instanceARecipients = new[] { userA };
        var instanceBRecipients = new[] { userA, userB };
        calls.Should().Contain(call =>
            call.BaseAddress == new Uri("http://instance-a") &&
            call.Request.RecipientUserIds.SequenceEqual(instanceARecipients));
        calls.Should().Contain(call =>
            call.BaseAddress == new Uri("http://instance-b") &&
            call.Request.RecipientUserIds.OrderBy(static id => id).SequenceEqual(instanceBRecipients.OrderBy(static id => id)));
    }

    [Fact]
    public async Task PublishPresenceChangeAsync_WhenRecipientsOffline_DoesNotCallInternalApi()
    {
        _userInstanceRoutingReaderMock
            .Setup(x => x.GetInstanceIdsByUserAsync(It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<Guid, IReadOnlyCollection<string>>());

        var router = CreateRouter();

        await router.PublishPresenceChangeAsync(
            new PublishPresenceChangeRequest
            {
                UserId = _fixture.Create<Guid>(),
                Status = PresenceStatus.Active,
                ChangedAtUtc = DateTimeOffset.UtcNow,
                RecipientUserIds = [_fixture.Create<Guid>()]
            },
            CancellationToken.None);

        _internalApiClientMock.Verify(
            x => x.PublishPresenceChangeAsync(It.IsAny<Uri>(), It.IsAny<PublishPresenceChangeRequest>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task PublishMessageAsync_WhenInstanceAddressMissing_ThrowsInvalidOperationException()
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

        var act = () => router.PublishMessageAsync(
            new PublishMessageRequest
            {
                MessageId = _fixture.Create<Guid>(),
                ConversationId = _fixture.Create<Guid>(),
                SenderUserId = _fixture.Create<Guid>(),
                SenderDisplayName = "Jane",
                Text = "Hello",
                SentAtUtc = DateTimeOffset.UtcNow,
                RecipientUserIds = [userId]
            },
            CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*missing mapping*");
    }

    private RealtimeEventRouter CreateRouter() =>
        new(
            _userInstanceRoutingReaderMock.Object,
            _addressResolverMock.Object,
            _internalApiClientMock.Object);
}
