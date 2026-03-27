using FlowChat.Shared.Application;
using FlowChat.UserProfileService.Infrastructure;
using FlowChat.UserProfileService.Infrastructure.Kafka;
using FlowChat.UserProfileService.Persistence;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Messaging.Publishing;

namespace FlowChat.UserProfileService.UnitTests;

public sealed class ApiSilverbackServiceRegistrationTests
{
    [Fact]
    public void AddApiSilverbackMessaging_RegistersPublisherAndIntegrationEventPublisher()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:UserProfileDb"] = "Host=localhost;Port=5432;Database=flowchat_userprofile_test_db;Username=flowchat_app;Password=flowchat_app_pw;",
                ["Kafka:UserProfileCreatedProducer:BootstrapServers"] = "localhost:9092",
                ["Kafka:UserProfileCreatedProducer:Topic"] = "dev.flowchat.user-profile.user-profile.v1",
                ["Kafka:UserProfileStateChangedProducer:BootstrapServers"] = "localhost:9092",
                ["Kafka:UserProfileStateChangedProducer:Topic"] = "dev.flowchat.user-profile.user-profile.v1"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddOptions();
        services.AddLogging();
        services.AddInfrastructureServices(configuration);
        services.AddPersistenceServices(configuration);
        services.AddApiSilverbackMessaging(configuration);

        using var serviceProvider = services.BuildServiceProvider();

        var publisher = serviceProvider.GetRequiredService<IPublisher>();
        var integrationEventPublisher = serviceProvider.GetRequiredService<IIntegrationEventPublisher>();

        Assert.NotNull(publisher);
        Assert.NotNull(integrationEventPublisher);
    }
}

