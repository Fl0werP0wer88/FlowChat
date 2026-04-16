using AutoMapper;
using FlowChat.AuthService.Application.Features.Account.Eventing.DomainEvents.AccountConfirmed;
using FlowChat.AuthService.Domain.Entities.Account;
using FlowChat.AuthService.Domain.Entities.Account.Events;
using FlowChat.Core.Messaging.AuthService.Events;
using FlowChat.Shared.Domain;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;

namespace FlowChat.AuthService.UnitTests;

public sealed class AccountConfirmedDomainEventToIntegrationEventProfileTests
{
    private readonly IMapper _mapper;

    public AccountConfirmedDomainEventToIntegrationEventProfileTests()
    {
        _mapper = new MapperConfiguration(
            cfg => cfg.AddProfile<AccountConfirmedDomainEventToIntegrationEventProfile>(),
            NullLoggerFactory.Instance).CreateMapper();
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
