using FlowChat.Core.Messaging;
using FlowChat.Core.Messaging.AuthService.Events;
using FlowChat.Core.Messaging.UserProfileService.ReadModels;
using FlowChat.Shared.Application;
using FlowChat.Shared.Infrastructure.Silverback.Kafka;
using FlowChat.UserProfileService.Infrastructure;
using FlowChat.UserProfileService.Infrastructure.Kafka;
using FlowChat.UserProfileService.Persistence;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Messaging.Publishing;

namespace FlowChat.UserProfileService.IntegrationTests;

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
                ["Kafka:UserEmailVerificationRequestedProducer:BootstrapServers"] = "localhost:9092",
                ["Kafka:UserEmailVerificationRequestedProducer:Topic"] = "dev.flowchat.notification.email.v1",
                ["Kafka:UserProfileStateChangedProducer:BootstrapServers"] = "localhost:9092",
                ["Kafka:UserProfileStateChangedProducer:Topic"] = "dev.flowchat.user-profile.user-profile.v1",
                ["Kafka:UserProfileProjectionProducer:BootstrapServers"] = "localhost:9092",
                ["Kafka:UserProfileProjectionProducer:Topic"] = "dev.flowchat.user-profile.user-profile.v1"
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
        var integrationEventPublisher = serviceProvider.GetRequiredService<IOutboxIntegrationEventPublisher>();
        var registry = serviceProvider.GetRequiredService<KafkaProducerSettingsRegistry>();
        var emailVerificationOptions = registry.Get<EmailVerificationRequestIntegrationEvent>();
        var projectionOptions = registry.Get<ProjectionIntegrationEvent<UserProfileReadModel>>();

        publisher.Should().NotBeNull();
        integrationEventPublisher.Should().NotBeNull();
        emailVerificationOptions!.Topic.Should().Be("dev.flowchat.notification.email.v1");
        projectionOptions!.Topic.Should().Be("dev.flowchat.user-profile.user-profile.v1");
    }
}
