using AutoMapper;
using FlowChat.AuthService.Application.Common.Eventing;
using FlowChat.AuthService.Application.Common.Eventing.Handlers;
using FlowChat.AuthService.Domain.Entities.Account;
using FlowChat.AuthService.Domain.Entities.Account.Events;
using FlowChat.Core.Messaging.AuthService.Events;
using FlowChat.Shared.Application;
using FlowChat.Shared.Domain;
using FlowChat.Shared.Domain.ValueObjects;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace FlowChat.AuthService.UnitTests;

public sealed class AccountRegisteredDomainEventHandlerTests
{
    private readonly Mock<IOutboxIntegrationEventPublisher> _publisherMock = new();
    private readonly IMapper _mapper = new MapperConfiguration(
        cfg => cfg.AddProfile<DomainEventToIntegrationEventProfile>(),
        NullLoggerFactory.Instance).CreateMapper();

    [Fact]
    public async Task Handle_MapsAndPublishesAccountRegisteredIntegrationEvent()
    {
        var handler = new AccountRegisteredDomainEventHandler(_publisherMock.Object, _mapper);
        var domainEvent = new AccountRegisteredDomainEvent(
            Id<Account>.New(),
            "flower",
            EmailAddress.Create("flower@example.com"),
            "Flower",
            "Power",
            "FlowChat");
        AccountRegisteredIntegrationEvent? capturedEvent = null;

        _publisherMock
            .Setup(x => x.Publish(It.IsAny<AccountRegisteredIntegrationEvent>(), It.IsAny<CancellationToken>()))
            .Callback<AccountRegisteredIntegrationEvent, CancellationToken>((integrationEvent, _) => capturedEvent = integrationEvent)
            .Returns(Task.CompletedTask);

        await handler.Handle(domainEvent, CancellationToken.None);

        capturedEvent.Should().NotBeNull();
        capturedEvent!.FriendlyUserId.Should().Be("flower");
        capturedEvent.Email.Should().Be("flower@example.com");
        capturedEvent.FirstName.Should().Be("Flower");
        capturedEvent.LastName.Should().Be("Power");
        capturedEvent.Organization.Should().Be("FlowChat");
    }
}
