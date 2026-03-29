using AutoMapper;
using FlowChat.AuthService.Application.Common.Eventing;
using FlowChat.AuthService.Domain.Entities;
using FlowChat.AuthService.Domain.Events;
using FlowChat.Core.Messaging.AuthService.Events;
using FlowChat.Shared.Domain;
using Microsoft.Extensions.Logging.Abstractions;

namespace FlowChat.AuthService.UnitTests;

public sealed class DomainEventToIntegrationEventProfileTests
{
    private readonly IMapper _mapper;

    public DomainEventToIntegrationEventProfileTests()
    {
        var configuration = new MapperConfiguration(
            cfg => cfg.AddProfile<DomainEventToIntegrationEventProfile>(),
            NullLoggerFactory.Instance);
        configuration.AssertConfigurationIsValid();
        _mapper = configuration.CreateMapper();
    }

    [Fact]
    public void AccountRegisteredDomainEvent_IsMappedToIntegrationEvent()
    {
        var userId = Id<Identity>.FromGuid(Guid.NewGuid());
        var domainEvent = new AccountRegisteredDomainEvent(
            userId,
            "flower",
            "flower@example.com",
            "+48123123123",
            "Flow",
            "Er");

        var integrationEvent = _mapper.Map<AccountRegisteredIntegrationEvent>(domainEvent);

        Assert.Equal(userId.Value.ToString(), integrationEvent.Key);
        Assert.Equal(userId.Value, integrationEvent.UserId);
        Assert.Equal("flower@example.com", integrationEvent.Email);
        Assert.Equal("+48123123123", integrationEvent.PhoneNumber);
        Assert.Equal("flower", integrationEvent.UserName);
        Assert.Equal("flower", integrationEvent.DisplayName);
        Assert.Equal("Flow", integrationEvent.FirstName);
        Assert.Equal("Er", integrationEvent.LastName);
    }

    [Fact]
    public void AccountConfirmedDomainEvent_IsMappedToIntegrationEvent()
    {
        var userId = Id<Identity>.FromGuid(Guid.NewGuid());
        var domainEvent = new AccountConfirmedDomainEvent(userId);

        var integrationEvent = _mapper.Map<AccountConfirmedIntegrationEvent>(domainEvent);

        Assert.Equal(userId.Value.ToString(), integrationEvent.Key);
        Assert.Equal(userId.Value, integrationEvent.UserId);
    }

    [Fact]
    public void PhoneNumberConfirmedDomainEvent_IsMappedToIntegrationEvent()
    {
        var userId = Id<Identity>.FromGuid(Guid.NewGuid());
        var domainEvent = new PhoneNumberConfirmedDomainEvent(userId, "+48123123123");

        var integrationEvent = _mapper.Map<PhoneNumberConfirmedIntegrationEvent>(domainEvent);

        Assert.Equal(userId.Value.ToString(), integrationEvent.Key);
        Assert.Equal(userId.Value, integrationEvent.UserId);
        Assert.Equal("+48123123123", integrationEvent.PhoneNumber);
    }
}
