using FlowChat.Core.Messaging.SocialGraphService.ReadModels;
using FlowChat.Core.Results;
using FlowChat.Shared.Application;
using FlowChat.Shared.Application.Common.Eventing;
using FlowChat.Shared.Application.CommandHandlers.AggregateRootCommandHandlerBaseV2.BeforeSaveProcessors;
using FlowChat.SocialGraphService.Application.Features.Contact.Commands.AddContact;
using FlowChat.SocialGraphService.Application.Features.Contact.Commands.DeleteContact;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using ContactAggregate = FlowChat.SocialGraphService.Domain.Entities.Contact.Contact;

namespace FlowChat.SocialGraphService.Application;

public static class ApplicationServiceRegistration
{
    public static IServiceCollection AddApiApplicationServices(this IServiceCollection services)
    {
        var applicationAssembly = typeof(ApplicationServiceRegistration).Assembly;

        services.AddAutoMapper((Action<AutoMapper.IMapperConfigurationExpression>?)null, applicationAssembly);
        services.AddFlowChatValidatorsFromAssembly(applicationAssembly);
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssemblies(applicationAssembly);
            cfg.AddFlowChatBehaviors();
        });
        services.AddScoped<ILocalEventDispatcher, LocalEventDispatcher>();
        services.AddContactProjectionBeforeSaveProcessors();

        return services;
    }

    public static IServiceCollection AddWorkerApplicationServices(this IServiceCollection services)
    {
        var applicationAssembly = typeof(ApplicationServiceRegistration).Assembly;

        services.AddAutoMapper((Action<AutoMapper.IMapperConfigurationExpression>?)null, applicationAssembly);
        services.AddFlowChatValidatorsFromAssembly(applicationAssembly);
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssemblies(applicationAssembly);
            cfg.AddFlowChatBehaviors();
        });
        services.AddScoped<ILocalEventDispatcher, LocalEventDispatcher>();
        services.AddContactProjectionBeforeSaveProcessors();

        return services;
    }

    private static IServiceCollection AddContactProjectionBeforeSaveProcessors(this IServiceCollection services)
    {
        services.AddContactProjectionBeforeSaveProcessor<AddContactCommand, Guid>();
        services.AddContactProjectionBeforeSaveProcessor<DeleteContactCommand, Unit>();

        return services;
    }

    private static IServiceCollection AddContactProjectionBeforeSaveProcessor<TCommand, TResponse>(this IServiceCollection services)
        where TCommand : ICommand<TResponse>, IRequest<FlowChatResult<TResponse>>
        where TResponse : notnull
    {
        services.AddScoped<
            IAggregateBeforeSaveProcessor<TCommand, ContactAggregate>,
            PublishProjectionIntegrationEventProcessor<TCommand, ContactAggregate, ContactReadModel>>();

        return services;
    }
}

