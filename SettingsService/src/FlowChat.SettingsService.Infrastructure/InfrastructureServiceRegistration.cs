//using FlowChat.SettingsService.Application.Contracts.Infrastructure;
//using FlowChat.SettingsService.Application.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FlowChat.SettingsService.Infrastructure;

public static class InfrastructureServiceRegistration
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
		
        return services;
    }
}
