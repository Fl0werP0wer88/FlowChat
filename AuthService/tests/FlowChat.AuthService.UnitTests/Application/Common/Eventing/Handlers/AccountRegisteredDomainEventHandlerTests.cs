using AutoMapper;
using FlowChat.AuthService.Application.Common.Eventing;
using FlowChat.AuthService.Application.Common.Eventing.Handlers;
using FlowChat.AuthService.Domain.Entities.Identity;
using FlowChat.AuthService.Domain.Events;
using FlowChat.Core.Messaging.AuthService.Events;
using FlowChat.Shared.Application;
using FlowChat.Shared.Domain;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace FlowChat.AuthService.UnitTests;

public sealed class AccountRegisteredDomainEventHandlerTests
{
    private readonly IMapper _mapper;
    private readonly Mock<IIntegrationEventPublisher> _publisherMock = new();

    public AccountRegisteredDomainEventHandlerTests()
    {
        _mapper = new MapperConfiguration(
                configuration => configuration.AddProfile<DomainEventToIntegrationEventProfile>(),
                NullLoggerFactory.Instance)
            .CreateMapper();

        _publisherMock
            .Setup(x => x.PublishToOutboxAsync(It.IsAny<AccountRegisteredIntegrationEvent>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
    }

    [Fact]
    public async Task Handle_MapsAndPublishesAccountRegisteredIntegrationEvent()
    {
        var handler = new AccountRegisteredDomainEventHandler(_publisherMock.Object, _mapper);
        var userId = Id<Identity>.New();
        var domainEvent = new AccountRegisteredDomainEvent(
            userId,
            "flower",
            "flower@example.com",
            "+48123123123",
            "Flow",
            "Er");

        AccountRegisteredIntegrationEvent? capturedEvent = null;
        _publisherMock
            .Setup(x => x.PublishToOutboxAsync(It.IsAny<AccountRegisteredIntegrationEvent>(), It.IsAny<CancellationToken>()))
            .Callback<AccountRegisteredIntegrationEvent, CancellationToken>((integrationEvent, _) => capturedEvent = integrationEvent)
            .Returns(Task.CompletedTask);

        await handler.Handle(domainEvent, CancellationToken.None);

        capturedEvent.Should().NotBeNull();
        capturedEvent!.Key.Should().Be(userId.Value.ToString());
        capturedEvent.UserId.Should().Be(userId.Value);
        capturedEvent.UserName.Should().Be("flower");
        capturedEvent.Email.Should().Be("flower@example.com");
        capturedEvent.PhoneNumber.Should().Be("+48123123123");
        capturedEvent.FirstName.Should().Be("Flow");
        capturedEvent.LastName.Should().Be("Er");
    }
}
