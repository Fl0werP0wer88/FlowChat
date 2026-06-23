using AutoMapper;
using FlowChat.Core.Messaging.UserProfileService.Events;
using FlowChat.Shared.Application;
using FlowChat.UserProfileService.Application.Features.UserProfile.EmailVerification.Interfaces;
using FlowChat.UserProfileService.Domain.Entities.UserProfile.Events;

namespace FlowChat.UserProfileService.Application.Features.UserProfile.Eventing.DomainEvents.UserProfileCreated;

public sealed class UserProfileCreatedDomainEventHandler(
    IOutboxIntegrationEventPublisher integrationEventPublisher,
    IMapper mapper,
    IEmailVerificationRequestIssuer emailVerificationRequestIssuer)
    : MappedDomainEventHandlerBase<UserProfileCreatedDomainEvent, UserProfileCreatedIntegrationEvent>(
        integrationEventPublisher,
        mapper)
{
    private readonly IEmailVerificationRequestIssuer _emailVerificationRequestIssuer = emailVerificationRequestIssuer;

    protected override string ResolveKafkaKey(
        UserProfileCreatedDomainEvent notification,
        UserProfileCreatedIntegrationEvent integrationEvent) =>
        notification.UserProfileId.Value.ToString();

    protected override Task ExecuteAsync(
        UserProfileCreatedDomainEvent notification,
        CancellationToken cancellationToken)
    {
        // Issuing the verification request is a side effect of profile creation — done here
        // rather than in the command handler so the domain event is the single source of truth
        // for triggering the verification flow (including replays).
        return _emailVerificationRequestIssuer.IssueAsync(
            notification.UserProfileId.Value,
            notification.MainEmailId.Value,
            notification.MainEmail.Value,
            cancellationToken);
    }
}
