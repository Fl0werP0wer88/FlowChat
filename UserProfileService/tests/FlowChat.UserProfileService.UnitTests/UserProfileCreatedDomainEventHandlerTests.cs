using AutoMapper;
using FlowChat.Application.Abstractions;
using FlowChat.Domain.Abstractions;
using FlowChat.Messaging.Contracts;
using FlowChat.Messaging.Contracts.UserProfileService.Events;
using FlowChat.UserProfileService.Application.Common.Eventing;
using FlowChat.UserProfileService.Application.Common.Eventing.Handlers;
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
        var handler = new UserProfileCreatedDomainEventHandler(publisher, mapper);
        var userProfileId = Id<UserProfile>.New();
        var domainEvent = new UserProfileCreatedDomainEvent(
            userProfileId,
            "jdoe",
            "John Doe",
            "john@example.com",
            "+48123123123",
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
}
