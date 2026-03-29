using AutoMapper;
using FlowChat.Shared.Application;
using FlowChat.Shared.Domain;
using FlowChat.Shared.Domain.ValueObjects;
using FlowChat.Core.Messaging;
using FlowChat.Core.Messaging.UserProfileService.Events;
using FlowChat.UserProfileService.Application.Common.Eventing;
using FlowChat.UserProfileService.Application.Common.Eventing.Handlers;
using FlowChat.UserProfileService.Domain.Entities;
using FlowChat.UserProfileService.Domain.Events;
using Microsoft.Extensions.Logging.Abstractions;

namespace FlowChat.UserProfileService.UnitTests;

public sealed class EmailConfirmedDomainEventHandlerTests
{
    [Fact]
    public async Task Handle_PublishesMappedUserEmailConfirmedIntegrationEvent()
    {
        var mapper = new MapperConfiguration(
                configuration => configuration.AddProfile<DomainEventToIntegrationEventProfile>(),
                NullLoggerFactory.Instance)
            .CreateMapper();
        var publisher = new CapturingIntegrationEventPublisher();
        var handler = new EmailConfirmedDomainEventHandler(publisher, mapper);
        var userProfileId = Id<UserProfile>.New();
        var emailId = Id<Email>.New();
        var domainEvent = new EmailConfirmedDomainEvent(
            userProfileId,
            emailId,
            EmailAddress.Create("john@example.com"));

        await handler.Handle(domainEvent, CancellationToken.None);

        var integrationEvent = Assert.IsType<UserEmailConfirmedIntegrationEvent>(Assert.Single(publisher.PublishedEvents));
        Assert.Equal(userProfileId.Value, integrationEvent.UserProfileId);
        Assert.Equal(emailId.Value, integrationEvent.EmailId);
        Assert.Equal("john@example.com", integrationEvent.Email);
        Assert.Equal(userProfileId.Value.ToString(), integrationEvent.Key);
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
