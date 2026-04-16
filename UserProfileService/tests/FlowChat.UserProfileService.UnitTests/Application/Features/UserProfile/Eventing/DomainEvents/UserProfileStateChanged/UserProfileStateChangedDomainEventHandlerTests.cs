using AutoMapper;
using FlowChat.Shared.Application;
using FlowChat.Shared.Domain;
using FlowChat.Core.Messaging;
using FlowChat.Core.Messaging.UserProfileService.Events;
using FlowChat.UserProfileService.Application.Common.Eventing;
using FlowChat.UserProfileService.Application.Features.UserProfile.Eventing.DomainEvents.UserProfileStateChanged;
using FlowChat.UserProfileService.Domain.Entities.EmailVerificationRequest;
using FlowChat.UserProfileService.Domain.Entities.UserProfile;
using Microsoft.Extensions.Logging.Abstractions;

namespace FlowChat.UserProfileService.UnitTests;

public sealed class UserProfileStateChangedDomainEventHandlerTests
{
    private readonly IMapper _mapper;
    private readonly Mock<IOutboxIntegrationEventPublisher> _publisherMock = new();

    public UserProfileStateChangedDomainEventHandlerTests()
    {
        _mapper = new MapperConfiguration(
                configuration => configuration.AddProfile<DomainEventToIntegrationEventProfile>(),
                NullLoggerFactory.Instance)
            .CreateMapper();

        _publisherMock
            .Setup(x => x.Publish(It.IsAny<UserProfileChangedIntegrationEvent>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
    }

    [Fact]
    public async Task Handle_PublishesMappedUserProfileStateChangedIntegrationEvent()
    {
        var handler = new UserProfileStateChangedDomainEventHandler(_publisherMock.Object, _mapper);
        var userProfileId = Id<UserProfile>.New();
        var domainEvent = new AggregateStateChangedDomainEvent<UserProfile, UserProfileState>(
            userProfileId,
            "user-profile-service.user-profile",
            new UserProfileState
            {
                Id = userProfileId.Value,
                FriendlyUserId = "jdoe",
                FirstName = "John",
                LastName = "Doe",
                Organization = "FlowChat",
                AvatarUrl = "https://cdn.example/avatar.png",
                Bio = "about me",
                IsActive = true,
                LastSeenAtUtc = new DateTimeOffset(2026, 3, 11, 9, 0, 0, TimeSpan.Zero),
                Emails =
                [
                    new UserProfileEmailState
                    {
                        Id = Guid.NewGuid(),
                        UserProfileId = userProfileId.Value,
                        Address = "john@example.com",
                        IsMain = true,
                        IsAuth = true,
                        IsConfirmed = true,
                        IsVisible = true
                    }
                ],
                Phones =
                [
                    new UserProfilePhoneState
                    {
                        Id = Guid.NewGuid(),
                        UserProfileId = userProfileId.Value,
                        Number = "+48123123123",
                        IsMain = true,
                        IsConfirmed = true,
                        IsVisible = true
                    }
                ]
            });

        UserProfileChangedIntegrationEvent? capturedEvent = null;
        _publisherMock
            .Setup(x => x.Publish(It.IsAny<UserProfileChangedIntegrationEvent>(), It.IsAny<CancellationToken>()))
            .Callback<UserProfileChangedIntegrationEvent, CancellationToken>((evt, _) => capturedEvent = evt)
            .Returns(Task.CompletedTask);

        await handler.Handle(domainEvent, CancellationToken.None);

        capturedEvent.Should().NotBeNull();
        capturedEvent!.UserProfileId.Should().Be(userProfileId.Value);
        capturedEvent.Key.Should().Be(userProfileId.Value.ToString());
        capturedEvent.FriendlyUserId.Should().Be("jdoe");
        capturedEvent.MainEmail.Should().NotBeNull();
        capturedEvent.MainEmail!.Address.Should().Be("john@example.com");
        capturedEvent.MainEmail.IsConfirmed.Should().BeTrue();
        capturedEvent.MainPhone.Should().NotBeNull();
        capturedEvent.MainPhone!.Number.Should().Be("+48123123123");
        capturedEvent.AvatarUrl.Should().Be("https://cdn.example/avatar.png");
        capturedEvent.Bio.Should().Be("about me");
        capturedEvent.IsActive.Should().BeTrue();
        capturedEvent.LastSeenAtUtc.Should().Be(new DateTimeOffset(2026, 3, 11, 9, 0, 0, TimeSpan.Zero));
        capturedEvent.FirstName.Should().Be("John");
        capturedEvent.LastName.Should().Be("Doe");
        capturedEvent.Organization.Should().Be("FlowChat");
    }
}
