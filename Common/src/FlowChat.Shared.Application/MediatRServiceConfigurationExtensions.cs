using FlowChat.Shared.Application.Behaviors;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace FlowChat.Shared.Application;

public static class MediatRServiceConfigurationExtensions
{
    public static MediatRServiceConfiguration AddFlowChatBehaviors(this MediatRServiceConfiguration configuration)
    {
        configuration.AddOpenBehavior(typeof(LoggingPipelineBehaviour<,>));
        configuration.AddOpenBehavior(typeof(RetryPipelineBehavior<,>));
        configuration.AddOpenBehavior(typeof(ExceptionHandlingPipelineBehavior<,>));
        configuration.AddOpenBehavior(typeof(ValidationPipelineBehaviour<,>));

        return configuration;
    }
}

