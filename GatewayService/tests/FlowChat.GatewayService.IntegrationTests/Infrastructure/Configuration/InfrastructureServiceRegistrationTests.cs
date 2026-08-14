using FluentAssertions;
using FlowChat.GatewayService.Infrastructure.Clients.ChatService;
using FlowChat.GatewayService.Infrastructure.Clients.PresenceService;
using FlowChat.GatewayService.Infrastructure.Configuration;
using FlowChat.GatewayService.Infrastructure.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FlowChat.GatewayService.IntegrationTests.Infrastructure.Configuration;

public sealed class InfrastructureServiceRegistrationTests
{
    [Fact]
    public void AddGatewayInfrastructure_ValidConfiguration_ResolvesClientsAndForwardingHandler()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["GatewayServices:ChatServiceBaseUrl"] = "https://chat.test",
                ["GatewayServices:ChatServiceInternalApiKey"] = "chat-key",
                ["GatewayServices:PresenceServiceBaseUrl"] = "https://presence.test",
                ["GatewayServices:PresenceServiceInternalApiKey"] = "presence-key"
            })
            .Build();
        var services = new ServiceCollection();

        services.AddGatewayInfrastructure(configuration);

        using var provider = services.BuildServiceProvider();
        provider.GetRequiredService<IChatServiceClient>().Should().NotBeNull();
        provider.GetRequiredService<IPresenceServiceClient>().Should().NotBeNull();
        provider.GetRequiredService<BearerTokenForwardingHandler>().Should().NotBeNull();
    }
}
