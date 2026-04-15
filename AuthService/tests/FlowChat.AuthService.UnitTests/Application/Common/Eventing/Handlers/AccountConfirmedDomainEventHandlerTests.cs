using AutoMapper;
using FlowChat.AuthService.Application.Common.Eventing;
using FlowChat.AuthService.Application.Common.Eventing.Handlers;
using FlowChat.AuthService.Domain.Entities.Account;
using FlowChat.AuthService.Domain.Entities.Account.Events;
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
        cfg => cfg.AddProfile<DomainEventToIntegrationEventProfile>(),
        NullLoggerFactory.Instance).CreateMapper();

    [Fact]
    public async Task Handle_MapsAndPublishesAccountConfirmedIntegrationEvent()
    {
        var handler = new AccountConfirmedDomainEventHandler(_publisherMock.Object, _mapper);
        var domainEvent = new AccountConfirmedDomainEvent(Id<Account>.New());
        AccountConfirmedIntegrationEvent? capturedEvent = null;

        _publisherMock
            .Setup(x => x.Publish(It.IsAny<AccountConfirmedIntegrationEvent>(), It.IsAny<CancellationToken>()))
            .Callback<AccountConfirmedIntegrationEvent, CancellationToken>((integrationEvent, _) => capturedEvent = integrationEvent)
            .Returns(Task.CompletedTask);

        await handler.Handle(domainEvent, CancellationToken.None);

        capturedEvent.Should().NotBeNull();
        capturedEvent!.UserId.Should().Be(domainEvent.AccountId.Value);
    }
}
