using AutoMapper;
using FlowChat.AuthService.Application.Features.Account.Eventing.DomainEvents.AccountRegistered;
using FlowChat.AuthService.Domain.Entities.Account;
using FlowChat.AuthService.Domain.Entities.Account.Events;
using FlowChat.Core.Messaging;
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
        cfg => cfg.AddProfile<AccountRegisteredDomainEventToIntegrationEventProfile>(),
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
        IntegrationEventEnvelope<AccountRegisteredIntegrationEvent>? capturedEnvelope = null;

        _publisherMock
            .Setup(x => x.PublishAsync(It.IsAny<IntegrationEventEnvelope<AccountRegisteredIntegrationEvent>>(), It.IsAny<CancellationToken>()))
            .Callback<IntegrationEventEnvelope<AccountRegisteredIntegrationEvent>, CancellationToken>((envelope, _) => capturedEnvelope = envelope)
            .Returns(Task.CompletedTask);

        await handler.Handle(domainEvent, CancellationToken.None);

        capturedEnvelope.Should().NotBeNull();
        capturedEnvelope!.KafkaKey.Should().Be(domainEvent.AccountId.Value.ToString());
        var capturedEvent = capturedEnvelope.Payload;
        capturedEvent.FriendlyUserId.Should().Be("flower");
        capturedEvent.Email.Should().Be("flower@example.com");
        capturedEvent.FirstName.Should().Be("Flower");
        capturedEvent.LastName.Should().Be("Power");
        capturedEvent.Organization.Should().Be("FlowChat");
    }
}
