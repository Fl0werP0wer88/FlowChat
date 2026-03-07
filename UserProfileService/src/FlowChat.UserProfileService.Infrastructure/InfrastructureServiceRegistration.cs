using FlowChat.UserProfileService.Application.Contracts.Mapping;
using FlowChat.UserProfileService.Infrastructure.Configuration;
using FlowChat.UserProfileService.Infrastructure.Mapping;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace FlowChat.UserProfileService.Infrastructure;

public static class InfrastructureServiceRegistration
{
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var infrastructureAssembly = typeof(InfrastructureServiceRegistration).Assembly;

        services.TryAddSingleton<IApiSettingsManager>(new ApiSettingsManager(configuration));
        services.AddAutoMapper((Action<AutoMapper.IMapperConfigurationExpression>?)null, infrastructureAssembly);
        services.AddScoped<IObjectMapper, AutoMapperObjectMapper>();

        return services;
    }
}
