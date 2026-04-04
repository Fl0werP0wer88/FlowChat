using AutoMapper;
using FlowChat.AuthService.Application.Common.Eventing;
using FlowChat.AuthService.Domain.Entities.Account;
using FlowChat.AuthService.Domain.Entities.Account.Events;
using FlowChat.Core.Messaging.AuthService.Events;
using FlowChat.Shared.Domain;
using FlowChat.Shared.Domain.ValueObjects;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;

namespace FlowChat.AuthService.UnitTests;

public sealed class DomainEventToIntegrationEventProfileTests
{
    private readonly IMapper _mapper;

    public DomainEventToIntegrationEventProfileTests()
    {
        _mapper = new MapperConfiguration(
            cfg => cfg.AddProfile<DomainEventToIntegrationEventProfile>(),
            NullLoggerFactory.Instance).CreateMapper();
    }

    [Fact]
    public void AccountRegisteredDomainEvent_IsMappedToIntegrationEvent()
    {
        var accountId = Id<Account>.New();
        var domainEvent = new AccountRegisteredDomainEvent(accountId, "flower", EmailAddress.Create("flower@example.com"));

        var integrationEvent = _mapper.Map<AccountRegisteredIntegrationEvent>(domainEvent);

        integrationEvent.UserId.Should().Be(accountId.Value);
        integrationEvent.FriendlyUserId.Should().Be("flower");
        integrationEvent.Email.Should().Be("flower@example.com");
        integrationEvent.DisplayName.Should().Be("flower");
    }

    [Fact]
    public void AccountConfirmedDomainEvent_IsMappedToIntegrationEvent()
    {
        var accountId = Id<Account>.New();
        var domainEvent = new AccountConfirmedDomainEvent(accountId);

        var integrationEvent = _mapper.Map<AccountConfirmedIntegrationEvent>(domainEvent);

        integrationEvent.UserId.Should().Be(accountId.Value);
    }
}
