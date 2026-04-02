using AutoFixture;
using AutoMapper;
using FlowChat.AuthService.Application.Common.Eventing;
using FlowChat.AuthService.Domain.Entities.Identity;
using FlowChat.AuthService.Domain.Entities.Identity.Events;
using FlowChat.Core.Messaging.AuthService.Events;
using FlowChat.Shared.Domain;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;

namespace FlowChat.AuthService.UnitTests;

public sealed class DomainEventToIntegrationEventProfileTests
{
    private readonly IFixture _fixture = new Fixture();
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
        var userId = Id<Identity>.FromGuid(_fixture.Create<Guid>());
        var domainEvent = new AccountRegisteredDomainEvent(
            userId,
            "flower",
            "flower@example.com",
            "+48123123123",
            "Flow",
            "Er");

        var integrationEvent = _mapper.Map<AccountRegisteredIntegrationEvent>(domainEvent);

        integrationEvent.Key.Should().Be(userId.Value.ToString());
        integrationEvent.UserId.Should().Be(userId.Value);
        integrationEvent.Email.Should().Be("flower@example.com");
        integrationEvent.PhoneNumber.Should().Be("+48123123123");
        integrationEvent.UserName.Should().Be("flower");
        integrationEvent.DisplayName.Should().Be("flower");
        integrationEvent.FirstName.Should().Be("Flow");
        integrationEvent.LastName.Should().Be("Er");
    }

    [Fact]
    public void AccountConfirmedDomainEvent_IsMappedToIntegrationEvent()
    {
        var userId = Id<Identity>.FromGuid(_fixture.Create<Guid>());
        var domainEvent = new AccountConfirmedDomainEvent(userId);

        var integrationEvent = _mapper.Map<AccountConfirmedIntegrationEvent>(domainEvent);

        integrationEvent.Key.Should().Be(userId.Value.ToString());
        integrationEvent.UserId.Should().Be(userId.Value);
    }

    [Fact]
    public void PhoneNumberConfirmedDomainEvent_IsMappedToIntegrationEvent()
    {
        var userId = Id<Identity>.FromGuid(_fixture.Create<Guid>());
        var domainEvent = new PhoneNumberConfirmedDomainEvent(userId, "+48123123123");

        var integrationEvent = _mapper.Map<PhoneNumberConfirmedIntegrationEvent>(domainEvent);

        integrationEvent.Key.Should().Be(userId.Value.ToString());
        integrationEvent.UserId.Should().Be(userId.Value);
        integrationEvent.PhoneNumber.Should().Be("+48123123123");
    }
}
