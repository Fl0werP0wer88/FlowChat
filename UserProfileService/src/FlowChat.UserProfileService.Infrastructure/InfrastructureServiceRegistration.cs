using FlowChat.UserProfileService.Application.Contracts.Mapping;
using FlowChat.UserProfileService.Infrastructure.Mapping;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FlowChat.UserProfileService.Infrastructure;

public static class InfrastructureServiceRegistration
{
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var infrastructureAssembly = typeof(InfrastructureServiceRegistration).Assembly;

        services.AddAutoMapper((Action<AutoMapper.IMapperConfigurationExpression>?)null, infrastructureAssembly);
        services.AddScoped<IObjectMapper, AutoMapperObjectMapper>();

        return services;
    }
}
