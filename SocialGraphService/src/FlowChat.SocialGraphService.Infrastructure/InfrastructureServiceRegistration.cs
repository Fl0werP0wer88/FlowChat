using FlowChat.SocialGraphService.Infrastructure.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace FlowChat.SocialGraphService.Infrastructure;

public static class InfrastructureServiceRegistration
{
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var infrastructureAssembly = typeof(InfrastructureServiceRegistration).Assembly;

        services.TryAddSingleton<IApiSettingsManager>(new ApiSettingsManager(configuration));
        services.AddAutoMapper((Action<AutoMapper.IMapperConfigurationExpression>?)null, infrastructureAssembly);

        return services;
    }
}
