using FlowChat.Shared.Infrastructure.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FlowChat.HarnessService.API.Configuration;

public static class APIServiceRegistration
{
    public static IServiceCollection AddApiSettings(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddSettingsSections(configuration, typeof(APIServiceRegistration).Assembly);
        return services;
    }
}
