using AutoMapper;
using FlowChat.Shared.Application;
using FlowChat.Shared.Domain;
using FlowChat.Shared.Domain.ValueObjects;
using FlowChat.Core.Messaging;
using FlowChat.Core.Messaging.UserProfileService.Events;
using FlowChat.UserProfileService.Application.Common.Eventing;
using FlowChat.UserProfileService.Application.Common.Eventing.Handlers;
using FlowChat.UserProfileService.Application.Contracts.Infrastructure;
using FlowChat.UserProfileService.Domain.Entities.EmailVerificationRequest;
using FlowChat.UserProfileService.Domain.Entities.UserProfile;
using FlowChat.UserProfileService.Domain.Entities.UserProfile.Events;
using Microsoft.Extensions.Logging.Abstractions;
using DomainEmail = FlowChat.UserProfileService.Domain.Entities.UserProfile.Email;

namespace FlowChat.UserProfileService.UnitTests;

public sealed class UserProfileCreatedDomainEventHandlerTests
{
    private readonly IMapper _mapper;
    private readonly Mock<IIntegrationEventPublisher> _publisherMock = new();
    private readonly Mock<IEmailVerificationRequestIssuer> _issuerMock = new();

    public UserProfileCreatedDomainEventHandlerTests()
    {
        _mapper = new MapperConfiguration(
                configuration => configuration.AddProfile<DomainEventToIntegrationEventProfile>(),
                NullLoggerFactory.Instance)
            .CreateMapper();

        _publisherMock
            .Setup(x => x.PublishToOutboxAsync(It.IsAny<UserProfileCreatedIntegrationEvent>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _issuerMock
            .Setup(x => x.IssueAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid userProfileId, Guid emailId, string _, CancellationToken _) =>
                EmailVerificationRequest.Create(userProfileId, emailId, Guid.NewGuid().ToString("N"), DateTimeOffset.UtcNow.AddHours(24)));
    }

    [Fact]
    public async Task Handle_PublishesMappedUserProfileCreatedIntegrationEvent()
    {
        var handler = new UserProfileCreatedDomainEventHandler(_publisherMock.Object, _mapper, _issuerMock.Object);
        var userProfileId = Id<UserProfile>.New();
        var mainEmailId = Id<DomainEmail>.New();
        var domainEvent = new UserProfileCreatedDomainEvent(
            userProfileId,
            mainEmailId,
            "jdoe",
            "John Doe",
            EmailAddress.Create("john@example.com"),
            PhoneNumber.Create("+48123123123"),
            "https://cdn.example/avatar.png",
            "about me",
            true,
            new DateTimeOffset(2026, 3, 10, 9, 0, 0, TimeSpan.Zero),
            true,
            false);

        UserProfileCreatedIntegrationEvent? capturedEvent = null;
        _publisherMock
            .Setup(x => x.PublishToOutboxAsync(It.IsAny<UserProfileCreatedIntegrationEvent>(), It.IsAny<CancellationToken>()))
            .Callback<UserProfileCreatedIntegrationEvent, CancellationToken>((evt, _) => capturedEvent = evt)
            .Returns(Task.CompletedTask);

        await handler.Handle(domainEvent, CancellationToken.None);

        capturedEvent.Should().NotBeNull();
        capturedEvent!.UserProfileId.Should().Be(userProfileId.Value);
        capturedEvent.Key.Should().Be(userProfileId.Value.ToString());
        capturedEvent.FriendlyUserId.Should().Be("jdoe");
        capturedEvent.DisplayName.Should().Be("John Doe");
        capturedEvent.MainEmail.Should().Be("john@example.com");
        capturedEvent.MainPhone.Should().Be("+48123123123");
        capturedEvent.AvatarUrl.Should().Be("https://cdn.example/avatar.png");
        capturedEvent.Bio.Should().Be("about me");
        capturedEvent.IsActive.Should().BeTrue();
        capturedEvent.LastSeenAtUtc.Should().Be(new DateTimeOffset(2026, 3, 10, 9, 0, 0, TimeSpan.Zero));
        capturedEvent.IsEmailVisible.Should().BeTrue();
        capturedEvent.IsPhoneVisible.Should().BeFalse();

        _issuerMock.Verify(x => x.IssueAsync(userProfileId.Value, mainEmailId.Value, "john@example.com", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithoutPhone_MapsNullMainPhone()
    {
        var handler = new UserProfileCreatedDomainEventHandler(_publisherMock.Object, _mapper, _issuerMock.Object);
        var userProfileId = Id<UserProfile>.New();
        var mainEmailId = Id<DomainEmail>.New();
        var domainEvent = new UserProfileCreatedDomainEvent(
            userProfileId,
            mainEmailId,
            "jdoe",
            "John Doe",
            EmailAddress.Create("john@example.com"),
            null,
            null,
            null,
            true,
            null,
            true,
            false);

        UserProfileCreatedIntegrationEvent? capturedEvent = null;
        _publisherMock
            .Setup(x => x.PublishToOutboxAsync(It.IsAny<UserProfileCreatedIntegrationEvent>(), It.IsAny<CancellationToken>()))
            .Callback<UserProfileCreatedIntegrationEvent, CancellationToken>((evt, _) => capturedEvent = evt)
            .Returns(Task.CompletedTask);

        await handler.Handle(domainEvent, CancellationToken.None);

        capturedEvent.Should().NotBeNull();
        capturedEvent!.MainPhone.Should().BeNull();
    }
}
