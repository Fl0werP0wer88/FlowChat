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

public sealed class AccountConfirmedDomainEventHandlerTests
{
    private readonly IMapper _mapper;
    private readonly Mock<IIntegrationEventPublisher> _publisherMock = new();

    public AccountConfirmedDomainEventHandlerTests()
    {
        _mapper = new MapperConfiguration(
                configuration => configuration.AddProfile<DomainEventToIntegrationEventProfile>(),
                NullLoggerFactory.Instance)
            .CreateMapper();

        _publisherMock
            .Setup(x => x.PublishToOutboxAsync(It.IsAny<AccountConfirmedIntegrationEvent>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
    }

    [Fact]
    public async Task Handle_MapsAndPublishesAccountConfirmedIntegrationEvent()
    {
        var handler = new AccountConfirmedDomainEventHandler(_publisherMock.Object, _mapper);
        var userId = Id<Identity>.New();
        var domainEvent = new AccountConfirmedDomainEvent(userId);

        AccountConfirmedIntegrationEvent? capturedEvent = null;
        _publisherMock
            .Setup(x => x.PublishToOutboxAsync(It.IsAny<AccountConfirmedIntegrationEvent>(), It.IsAny<CancellationToken>()))
            .Callback<AccountConfirmedIntegrationEvent, CancellationToken>((integrationEvent, _) => capturedEvent = integrationEvent)
            .Returns(Task.CompletedTask);

        await handler.Handle(domainEvent, CancellationToken.None);

        capturedEvent.Should().NotBeNull();
        capturedEvent!.Key.Should().Be(userId.Value.ToString());
        capturedEvent.UserId.Should().Be(userId.Value);
    }
}
