using AutoMapper;
using FlowChat.AuthService.Application.Common.Eventing;
using FlowChat.AuthService.Application.Common.Eventing.Handlers;
using FlowChat.AuthService.Domain.Entities.Identity;
using FlowChat.AuthService.Domain.Entities.Identity.Events;
using FlowChat.Core.Messaging.AuthService.Events;
using FlowChat.Shared.Application;
using FlowChat.Shared.Domain;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace FlowChat.AuthService.UnitTests;

public sealed class PhoneNumberConfirmedDomainEventHandlerTests
{
    private readonly IMapper _mapper;
    private readonly Mock<IIntegrationEventPublisher> _publisherMock = new();

    public PhoneNumberConfirmedDomainEventHandlerTests()
    {
        _mapper = new MapperConfiguration(
                configuration => configuration.AddProfile<DomainEventToIntegrationEventProfile>(),
                NullLoggerFactory.Instance)
            .CreateMapper();

        _publisherMock
            .Setup(x => x.PublishToOutboxAsync(It.IsAny<PhoneNumberConfirmedIntegrationEvent>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
    }

    [Fact]
    public async Task Handle_MapsAndPublishesPhoneNumberConfirmedIntegrationEvent()
    {
        var handler = new PhoneNumberConfirmedDomainEventHandler(_publisherMock.Object, _mapper);
        var userId = Id<Identity>.New();
        var domainEvent = new PhoneNumberConfirmedDomainEvent(userId, "+48123123123");

        PhoneNumberConfirmedIntegrationEvent? capturedEvent = null;
        _publisherMock
            .Setup(x => x.PublishToOutboxAsync(It.IsAny<PhoneNumberConfirmedIntegrationEvent>(), It.IsAny<CancellationToken>()))
            .Callback<PhoneNumberConfirmedIntegrationEvent, CancellationToken>((integrationEvent, _) => capturedEvent = integrationEvent)
            .Returns(Task.CompletedTask);

        await handler.Handle(domainEvent, CancellationToken.None);

        capturedEvent.Should().NotBeNull();
        capturedEvent!.Key.Should().Be(userId.Value.ToString());
        capturedEvent.UserId.Should().Be(userId.Value);
        capturedEvent.PhoneNumber.Should().Be("+48123123123");
    }
}
