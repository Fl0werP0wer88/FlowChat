using AutoFixture;
using FlowChat.NotificationService.Application.Contracts.Persistence;
using FlowChat.NotificationService.Application.Features.Notification.Queries.GetNotifications;
using FlowChat.NotificationService.Domain.Enums;
using FluentAssertions;
using Moq;

namespace FlowChat.NotificationService.UnitTests.Application.Notifications.Queries.GetNotifications;

public sealed class GetNotificationsQueryHandlerTests
{
    private readonly IFixture _fixture = new Fixture();
    private readonly Mock<INotificationReadRepository> _repositoryMock = new();
    private readonly GetNotificationsQueryHandler _handler;

    public GetNotificationsQueryHandlerTests()
    {
        _handler = new GetNotificationsQueryHandler(_repositoryMock.Object);
    }

    [Fact]
    public async Task Handle_WhenUserIdIsProvided_ReturnsNotificationsForUser()
    {
        var userId = _fixture.Create<Guid>();
        var expected = new List<NotificationDto>
        {
            new(Guid.NewGuid(), userId, "a@b.com", "Alice", "Please confirm your email", NotificationType.EmailVerification,
                NotificationStatus.Sent, "msg-1", null, "key-1", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow)
        };

        _repositoryMock
            .Setup(x => x.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var result = await _handler.Handle(new GetNotificationsQuery(userId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEquivalentTo(expected);
        _repositoryMock.Verify(x => x.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()), Times.Once);
        _repositoryMock.Verify(x => x.GetRecentAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenUserIdIsNull_ReturnsRecentNotifications()
    {
        var expected = new List<NotificationDto>
        {
            new(Guid.NewGuid(), Guid.NewGuid(), "x@y.com", "Bob", "Welcome to FlowChat", NotificationType.Welcome,
                NotificationStatus.Pending, null, null, null, null, DateTimeOffset.UtcNow)
        };

        _repositoryMock
            .Setup(x => x.GetRecentAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var result = await _handler.Handle(new GetNotificationsQuery(null), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEquivalentTo(expected);
        _repositoryMock.Verify(x => x.GetRecentAsync(It.IsAny<CancellationToken>()), Times.Once);
        _repositoryMock.Verify(x => x.GetByUserIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenRepositoryReturnsEmpty_ReturnsSuccessWithEmptyList()
    {
        _repositoryMock
            .Setup(x => x.GetRecentAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<NotificationDto>());

        var result = await _handler.Handle(new GetNotificationsQuery(null), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }
}
