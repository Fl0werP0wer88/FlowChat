using FlowChat.Shared.Application;
using FlowChat.Shared.Application.Common.Eventing;
using FlowChat.Shared.Application.CommandHandlers.AggregateRootCommandHandlerBaseV2.BeforeSaveProcessors;
using FlowChat.Core.Results;
using FlowChat.UserProfileService.Application.Features.UserProfile.Commands.AddEmail;
using FlowChat.UserProfileService.Application.Features.UserProfile.Commands.AddPhone;
using FlowChat.UserProfileService.Application.Features.UserProfile.Commands.ConfirmEmailVerification;
using FlowChat.UserProfileService.Application.Features.UserProfile.Commands.CreateInitialUserProfile;
using FlowChat.UserProfileService.Application.Features.UserProfile.Commands.DeleteProfile;
using FlowChat.UserProfileService.Application.Features.UserProfile.Commands.SetAuthEmail;
using FlowChat.UserProfileService.Application.Features.UserProfile.Commands.SetMainEmail;
using FlowChat.UserProfileService.Application.Features.UserProfile.Commands.SetMainPhone;
using FlowChat.UserProfileService.Application.Features.UserProfile.Commands.UpdateProfile;
using FlowChat.UserProfileService.Application.Features.UserProfile.EmailVerification;
using FlowChat.UserProfileService.Application.Features.UserProfile.EmailVerification.Interfaces;
using FlowChat.UserProfileService.Application.Features.UserProfile.Processors;
using Microsoft.Extensions.DependencyInjection;
using MediatR;
using DomainUserProfile = FlowChat.UserProfileService.Domain.Entities.UserProfile.UserProfile;

namespace FlowChat.UserProfileService.Application;

public static class ApiApplicationServiceRegistration
{
    public static IServiceCollection AddApiApplicationServices(this IServiceCollection services)
    {
        var applicationAssembly = typeof(ApiApplicationServiceRegistration).Assembly;

        services.AddFlowChatValidatorsFromAssembly(applicationAssembly);
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssemblies(applicationAssembly);
            cfg.AddFlowChatBehaviors();
        });
        services.AddAutoMapper((Action<AutoMapper.IMapperConfigurationExpression>?)null, applicationAssembly);
        services.AddScoped<ILocalEventDispatcher, LocalEventDispatcher>();
        services.AddScoped<IEmailVerificationRequestIssuer, EmailVerificationRequestIssuer>();
        services.AddUserProfileProjectionBeforeSaveProcessors();

        return services;
    }
}

public static class ConsumerApplicationServiceRegistration
{
    public static IServiceCollection AddConsumerApplicationServices(this IServiceCollection services)
    {
        var consumerAssembly = typeof(ConsumerApplicationServiceRegistration).Assembly;

        services.AddFlowChatValidatorsFromAssembly(consumerAssembly);
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssemblies(consumerAssembly);
            cfg.AddFlowChatBehaviors();
        });
        services.AddAutoMapper((Action<AutoMapper.IMapperConfigurationExpression>?)null, consumerAssembly);
        services.AddScoped<ILocalEventDispatcher, LocalEventDispatcher>();
        services.AddScoped<IEmailVerificationRequestIssuer, EmailVerificationRequestIssuer>();
        services.AddUserProfileProjectionBeforeSaveProcessors();

        return services;
    }
}

internal static class CommonApplicationServiceRegistration
{
    public static IServiceCollection AddUserProfileProjectionBeforeSaveProcessors(this IServiceCollection services)
    {
        services.AddUserProfileProjectionBeforeSaveProcessor<CreateInitialUserProfileCommand, Guid>();
        services.AddUserProfileProjectionBeforeSaveProcessor<AddEmailCommand, Guid>();
        services.AddUserProfileProjectionBeforeSaveProcessor<AddPhoneCommand, Guid>();
        services.AddUserProfileProjectionBeforeSaveProcessor<ConfirmEmailVerificationCommand, IdempotentCommandResult<Unit>>();
        services.AddUserProfileProjectionBeforeSaveProcessor<SetAuthEmailCommand, Guid>();
        services.AddUserProfileProjectionBeforeSaveProcessor<SetMainEmailCommand, Guid>();
        services.AddUserProfileProjectionBeforeSaveProcessor<SetMainPhoneCommand, Guid>();
        services.AddUserProfileProjectionBeforeSaveProcessor<UpdateProfileCommand, Guid>();
        services.AddUserProfileProjectionBeforeSaveProcessor<DeleteProfileCommand, Guid>();

        return services;
    }

    private static IServiceCollection AddUserProfileProjectionBeforeSaveProcessor<TCommand, TResponse>(this IServiceCollection services)
        where TCommand : ICommand<TResponse>, IRequest<FlowChatResult<TResponse>>
        where TResponse : notnull
    {
        services.AddScoped<
            IAggregateBeforeSaveProcessor<TCommand, DomainUserProfile>,
            UserProfileProjectionProcessor<TCommand>>();

        return services;
    }
}

