using FlowChat.GatewayService.Infrastructure.Clients.ChatService;
using FlowChat.GatewayService.Infrastructure.Clients.PresenceService;
using FlowChat.GatewayService.Infrastructure.Configuration.Settings;
using FlowChat.GatewayService.Infrastructure.Http;
using FlowChat.Shared.Infrastructure.Configuration;
using FlowChat.Shared.Infrastructure.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FlowChat.GatewayService.Infrastructure.Configuration;

public static class InfrastructureServiceRegistration
{
    public static IServiceCollection AddGatewayInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddSettingsSections(configuration, typeof(InfrastructureServiceRegistration).Assembly);

        var settings = configuration
            .GetSection(new GatewayServicesSettingsSection().SectionName)
            .Get<GatewayServicesSettingsSection>() ?? new GatewayServicesSettingsSection();

        services.AddHttpContextAccessor();
        services.AddTransient<BearerTokenForwardingHandler>();

        services
            .AddFlowChatHttpClient<IChatServiceClient, ChatServiceClient>((_, client) =>
            {
                client.BaseAddress = new Uri(settings.ChatServiceBaseUrl);
                if (!string.IsNullOrWhiteSpace(settings.ChatServiceInternalApiKey))
                {
                    client.DefaultRequestHeaders.Add(
                        FlowChatHttpClientBase.InternalApiKeyHeaderName,
                        settings.ChatServiceInternalApiKey);
                }
            })
            .AddHttpMessageHandler<BearerTokenForwardingHandler>();

        services.AddFlowChatHttpClient<IPresenceServiceClient, PresenceServiceClient>((_, client) =>
        {
            client.BaseAddress = new Uri(settings.PresenceServiceBaseUrl);
            if (!string.IsNullOrWhiteSpace(settings.PresenceServiceInternalApiKey))
            {
                client.DefaultRequestHeaders.Add(
                    PresenceServiceClient.ApiKeyHeaderName,
                    settings.PresenceServiceInternalApiKey);
            }
        });

        return services;
    }
}
