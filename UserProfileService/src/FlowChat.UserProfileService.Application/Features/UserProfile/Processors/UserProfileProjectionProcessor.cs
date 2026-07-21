using AutoMapper;
using FlowChat.Core.Messaging;
using FlowChat.Core.Messaging.UserProfileService.ReadModels;
using FlowChat.Shared.Application;
using FlowChat.Shared.Application.CommandHandlers.AggregateRootCommandHandlerBaseV2.BeforeSaveProcessors;
using DomainUserProfile = FlowChat.UserProfileService.Domain.Entities.UserProfile.UserProfile;

namespace FlowChat.UserProfileService.Application.Features.UserProfile.Processors;

public sealed class UserProfileProjectionProcessor<TCommand>(
    IMapper mapper,
    IOutboxIntegrationEventPublisher integrationEventPublisher)
    : PublishProjectionIntegrationEventProcessorV2<TCommand, DomainUserProfile, UserProfileReadModel>(
        mapper,
        integrationEventPublisher);
