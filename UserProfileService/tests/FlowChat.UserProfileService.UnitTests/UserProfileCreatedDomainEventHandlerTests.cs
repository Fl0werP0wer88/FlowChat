using AutoMapper;
using FlowChat.Shared.Application;
using FlowChat.Shared.Domain;
using FlowChat.Shared.Domain.ValueObjects;
using FlowChat.Core.Messaging;
using FlowChat.Core.Messaging.UserProfileService.Events;
using FlowChat.UserProfileService.Application.Common.Eventing;
using FlowChat.UserProfileService.Application.Common.Eventing.Handlers;
using FlowChat.UserProfileService.Application.Contracts.Infrastructure;
using FlowChat.UserProfileService.Domain.Entities;
using FlowChat.UserProfileService.Domain.Events;
using Microsoft.Extensions.Logging.Abstractions;

namespace FlowChat.UserProfileService.UnitTests;

public sealed class UserProfileCreatedDomainEventHandlerTests
{
    [Fact]
    public async Task Handle_PublishesMappedUserProfileCreatedIntegrationEvent()
    {
        var mapper = new MapperConfiguration(
                configuration => configuration.AddProfile<DomainEventToIntegrationEventProfile>(),
                NullLoggerFactory.Instance)
            .CreateMapper();
        var publisher = new CapturingIntegrationEventPublisher();
        var issuer = new CapturingEmailVerificationRequestIssuer();
        var handler = new UserProfileCreatedDomainEventHandler(publisher, mapper, issuer);
        var userProfileId = Id<UserProfile>.New();
        var mainEmailId = Id<Email>.New();
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
            new DateTime(2026, 3, 10, 9, 0, 0, DateTimeKind.Utc),
            true,
            false);

        await handler.Handle(domainEvent, CancellationToken.None);

        var integrationEvent = Assert.IsType<UserProfileCreatedIntegrationEvent>(Assert.Single(publisher.PublishedEvents));
        Assert.Equal(userProfileId.Value, integrationEvent.UserProfileId);
        Assert.Equal(userProfileId.Value.ToString(), integrationEvent.Key);
        Assert.Equal("jdoe", integrationEvent.UserName);
        Assert.Equal("John Doe", integrationEvent.DisplayName);
        Assert.Equal("john@example.com", integrationEvent.MainEmail);
        Assert.Equal("+48123123123", integrationEvent.MainPhone);
        Assert.Equal("https://cdn.example/avatar.png", integrationEvent.AvatarUrl);
        Assert.Equal("about me", integrationEvent.Bio);
        Assert.True(integrationEvent.IsActive);
        Assert.Equal(new DateTime(2026, 3, 10, 9, 0, 0, DateTimeKind.Utc), integrationEvent.LastSeenAtUtc);
        Assert.True(integrationEvent.IsEmailVisible);
        Assert.False(integrationEvent.IsPhoneVisible);
        Assert.Equal(userProfileId.Value, issuer.LastUserProfileId);
        Assert.Equal(mainEmailId.Value, issuer.LastEmailId);
        Assert.Equal("john@example.com", issuer.LastEmailAddress);
    }

    private sealed class CapturingIntegrationEventPublisher : IIntegrationEventPublisher
    {
        public List<IntegrationEvent> PublishedEvents { get; } = [];

        public Task PublishToOutboxAsync<TEvent>(TEvent message, CancellationToken cancellationToken)
            where TEvent : IntegrationEvent
        {
            PublishedEvents.Add(message);
            return Task.CompletedTask;
        }
    }

    private sealed class CapturingEmailVerificationRequestIssuer : IEmailVerificationRequestIssuer
    {
        public Guid? LastUserProfileId { get; private set; }
        public Guid? LastEmailId { get; private set; }
        public string? LastEmailAddress { get; private set; }

        public Task<EmailVerificationRequest> IssueAsync(
            Guid userProfileId,
            Guid emailId,
            string emailAddress,
            CancellationToken cancellationToken)
        {
            LastUserProfileId = userProfileId;
            LastEmailId = emailId;
            LastEmailAddress = emailAddress;

            return Task.FromResult(EmailVerificationRequest.Create(
                userProfileId,
                emailId,
                Guid.NewGuid().ToString("N"),
                DateTime.UtcNow.AddHours(24)));
        }
    }
}
