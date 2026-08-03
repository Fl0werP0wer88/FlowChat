using AutoMapper;
using FlowChat.ChatService.Application.Features.UserProfile;
using FlowChat.Core.Exceptions;
using FlowChat.Core.Messaging;
using FlowChat.Core.Messaging.UserProfileService.ReadModels;
using FlowChat.Shared.Consumers.Projections.Single;

namespace FlowChat.ChatService.Consumers.Kafka.Projections;

public sealed class UserProfileProjectionValueFactory(IMapper mapper)
    : IProjectionSingleValueFactory<UserProfileReadModel, UserProfileProjectionDto>
{
    public UserProfileProjectionDto MapValue(
        ProjectionIntegrationEvent<UserProfileReadModel> message)
    {
        try
        {
            return mapper.Map<UserProfileProjectionDto>(message.Value);
        }
        catch (AutoMapperMappingException exception) when (exception.InnerException is NonTransientException nonTransientException)
        {
            throw nonTransientException;
        }
    }
}
