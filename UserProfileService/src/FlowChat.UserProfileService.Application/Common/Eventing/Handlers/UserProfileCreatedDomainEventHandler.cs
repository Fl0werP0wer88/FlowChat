using AutoMapper;
using FlowChat.Core.Messaging.UserProfileService.Events;
using FlowChat.Shared.Application;
using FlowChat.UserProfileService.Application.Contracts.Infrastructure;
using FlowChat.UserProfileService.Domain.Events;

namespace FlowChat.UserProfileService.Application.Common.Eventing.Handlers;

public sealed class UserProfileCreatedDomainEventHandler(
    IIntegrationEventPublisher integrationEventPublisher,
    IMapper mapper,
    IEmailVerificationRequestIssuer emailVerificationRequestIssuer)
    : MappedDomainEventHandlerBase<UserProfileCreatedDomainEvent, UserProfileCreatedIntegrationEvent>(
        integrationEventPublisher,
        mapper)
{
    private readonly IEmailVerificationRequestIssuer _emailVerificationRequestIssuer = emailVerificationRequestIssuer;

    protected override Task ExecuteAsync(
        UserProfileCreatedDomainEvent notification,
        CancellationToken cancellationToken)
    {
        return _emailVerificationRequestIssuer.IssueAsync(
            notification.UserProfileId.Value,
            notification.MainEmailId.Value,
            notification.MainEmail.Value,
            cancellationToken);
    }
}
