using FlowChat.Application.Abstractions.Behaviors;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace FlowChat.Application.Abstractions;

public static class MediatRServiceConfigurationExtensions
{
    public static MediatRServiceConfiguration AddFlowChatBehaviors(this MediatRServiceConfiguration configuration)
    {
        configuration.AddOpenBehavior(typeof(LoggingPipelineBehaviour<,>));

        return configuration;
    }
}
