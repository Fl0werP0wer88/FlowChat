using System.Diagnostics.CodeAnalysis;
using AutoMapper;
using FlowChat.ChatService.Application.Features.UserProfile;
using FlowChat.Core.Exceptions;
using FlowChat.Core.Messaging;
using FlowChat.Core.Messaging.UserProfileService.ReadModels;
using FlowChat.Shared.Application;
using FlowChat.Shared.Consumers.Projections.Single;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FlowChat.ChatService.Consumers.Kafka.Projections;

public sealed class UserProfileProjectionSubscriber(
    IMediator mediator,
    IConsumedOffsetCommitter consumedOffsetCommitter,
    IMapper mapper,
    ILogger<UserProfileProjectionSubscriber> logger)
    : ProjectionSingleSubscriberBase<UserProfileReadModel, UserProfileProjectionDto>(
        mediator,
        consumedOffsetCommitter,
        logger)
{
    protected override bool TryMapValue(
        ProjectionIntegrationEvent<UserProfileReadModel> message,
        [NotNullWhen(true)] out UserProfileProjectionDto? value)
    {
        try
        {
            value = mapper.Map<UserProfileProjectionDto>(message.Value);
            return true;
        }
        catch (AutoMapperMappingException exception) when (exception.InnerException is NonTransientException nonTransientException)
        {
            throw nonTransientException;
        }
    }
}
