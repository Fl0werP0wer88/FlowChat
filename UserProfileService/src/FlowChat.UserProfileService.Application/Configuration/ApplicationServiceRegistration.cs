using FlowChat.Shared.Application;
using FlowChat.Shared.Application.Common.Eventing;
using FlowChat.UserProfileService.Application.Features.UserProfile.Commands.CreateInitialUserProfile;
using FlowChat.UserProfileService.Application.Features.UserProfile.EmailVerification;
using FlowChat.UserProfileService.Application.Features.UserProfile.EmailVerification.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace FlowChat.UserProfileService.Application;

public static class ApplicationServiceRegistration
{
    public static IServiceCollection AddApiApplicationServices(this IServiceCollection services)
    {
        var applicationAssembly = typeof(ApplicationServiceRegistration).Assembly;

        services.AddFlowChatValidatorsFromAssembly(applicationAssembly);
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssemblies(applicationAssembly);
            cfg.AddFlowChatBehaviors();
        });
        services.AddAutoMapper((Action<AutoMapper.IMapperConfigurationExpression>?)null, applicationAssembly);
        services.AddScoped<IDomainEventDispatcher, FlowChatDomainEventDispatcher>();
        services.AddScoped<IEmailVerificationRequestIssuer, EmailVerificationRequestIssuer>();

        return services;
    }

    public static IServiceCollection AddWorkerApplicationServices(this IServiceCollection services)
    {
        var consumerAssembly = typeof(CreateInitialUserProfileCommandHandler).Assembly;

        services.AddFlowChatValidatorsFromAssembly(consumerAssembly);
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssemblies(consumerAssembly);
            cfg.AddFlowChatBehaviors();
        });
        services.AddAutoMapper((Action<AutoMapper.IMapperConfigurationExpression>?)null, consumerAssembly);
        services.AddScoped<IDomainEventDispatcher, FlowChatDomainEventDispatcher>();
        services.AddScoped<IEmailVerificationRequestIssuer, EmailVerificationRequestIssuer>();

        return services;
    }
}

