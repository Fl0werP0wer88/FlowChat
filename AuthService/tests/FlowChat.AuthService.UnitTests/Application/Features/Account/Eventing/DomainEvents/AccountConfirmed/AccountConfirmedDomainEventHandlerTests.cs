using AutoMapper;
using FlowChat.AuthService.Application.Features.Account.Eventing.DomainEvents.AccountConfirmed;
using FlowChat.AuthService.Domain.Entities.Account;
using FlowChat.AuthService.Domain.Entities.Account.Events;
using FlowChat.Core.Messaging;
using FlowChat.Core.Messaging.AuthService.Events;
using FlowChat.Shared.Application;
using FlowChat.Shared.Domain;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace FlowChat.AuthService.UnitTests;

public sealed class AccountConfirmedDomainEventHandlerTests
{
    private readonly Mock<IOutboxIntegrationEventPublisher> _publisherMock = new();
    private readonly IMapper _mapper = new MapperConfiguration(
        cfg => cfg.AddProfile<AccountConfirmedDomainEventToIntegrationEventProfile>(),
        NullLoggerFactory.Instance).CreateMapper();

    [Fact]
    public async Task Handle_MapsAndPublishesAccountConfirmedIntegrationEvent()
    {
        var handler = new AccountConfirmedDomainEventHandler(_publisherMock.Object, _mapper);
        var domainEvent = new AccountConfirmedDomainEvent(Id<Account>.New());
        IntegrationEventEnvelope<AccountConfirmedIntegrationEvent>? capturedEnvelope = null;

        _publisherMock
            .Setup(x => x.PublishAsync(It.IsAny<IntegrationEventEnvelope<AccountConfirmedIntegrationEvent>>(), It.IsAny<CancellationToken>()))
            .Callback<IntegrationEventEnvelope<AccountConfirmedIntegrationEvent>, CancellationToken>((envelope, _) => capturedEnvelope = envelope)
            .Returns(Task.CompletedTask);

        await handler.Handle(domainEvent, CancellationToken.None);

        capturedEnvelope.Should().NotBeNull();
        capturedEnvelope!.KafkaKey.Should().Be(domainEvent.AccountId.Value.ToString());
        capturedEnvelope.Payload.UserId.Should().Be(domainEvent.AccountId.Value);
    }
}
