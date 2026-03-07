using AutoMapper;
using FlowChat.AuthService.Domain.Entities;
using FlowChat.AuthService.Domain.Events;
using FlowChat.AuthService.Infrastructure.Kafka;
using FlowChat.Domain.Abstractions;
using FlowChat.Messaging.Contracts;
using FlowChat.Messaging.Contracts.AuthService.Events;
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
    public void UserCreatedDomainEvent_IsMappedToEnvelope()
    {
        var userId = Id<Identity>.FromGuid(Guid.NewGuid());
        var domainEvent = new UserCreatedDomainEvent(
            userId,
            "flower",
            "flower@example.com",
            "+48123123123",
            "Flow",
            "Er");

        var envelope = _mapper.Map<IntegrationEventEnvelope<UserCreatedIntegrationEvent>>(domainEvent);

        Assert.Equal(userId.Value.ToString(), envelope.KafkaKey);
        Assert.Equal(userId.Value, envelope.Payload.UserId);
        Assert.Equal("flower@example.com", envelope.Payload.Email);
        Assert.Equal("+48123123123", envelope.Payload.PhoneNumber);
        Assert.Equal("flower", envelope.Payload.UserName);
        Assert.Equal("flower", envelope.Payload.DisplayName);
        Assert.Equal("Flow", envelope.Payload.FirstName);
        Assert.Equal("Er", envelope.Payload.LastName);
    }

    [Fact]
    public void AccountConfirmedDomainEvent_IsMappedToEnvelope()
    {
        var userId = Id<Identity>.FromGuid(Guid.NewGuid());
        var domainEvent = new AccountConfirmedDomainEvent(userId);

        var envelope = _mapper.Map<IntegrationEventEnvelope<UserConfirmedIntegrationEvent>>(domainEvent);

        Assert.Equal(userId.Value.ToString(), envelope.KafkaKey);
        Assert.Equal(userId.Value, envelope.Payload.UserId);
    }

    [Fact]
    public void EmailVerificationRequestedDomainEvent_IsMappedToEnvelope()
    {
        var userId = Id<Identity>.FromGuid(Guid.NewGuid());
        var domainEvent = new EmailVerificationRequestedDomainEvent(
            userId,
            "flower@example.com",
            "https://localhost/confirm");

        var envelope = _mapper.Map<IntegrationEventEnvelope<EmailVerificationRequestIntegrationEvent>>(domainEvent);

        Assert.Equal(userId.Value.ToString(), envelope.KafkaKey);
        Assert.Equal(userId.Value, envelope.Payload.UserId);
        Assert.Equal("flower@example.com", envelope.Payload.UserEmail);
        Assert.Equal("https://localhost/confirm", envelope.Payload.ConfirmationLink);
    }
}
