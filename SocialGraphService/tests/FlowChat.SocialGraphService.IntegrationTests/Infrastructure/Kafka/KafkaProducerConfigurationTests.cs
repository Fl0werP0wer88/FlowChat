using FlowChat.Core.Messaging;
using FlowChat.Core.Messaging.SocialGraphService.ReadModels;
using FlowChat.Shared.Infrastructure.Silverback.Kafka;
using FlowChat.SocialGraphService.Infrastructure;
using FlowChat.SocialGraphService.Infrastructure.Configuration.Settings;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace FlowChat.SocialGraphService.IntegrationTests.Infrastructure.Kafka;

public sealed class KafkaProducerConfigurationTests
{
    [Fact]
    public void AddApiInfrastructureServices_ResolvesContactProjectionProducerOptions()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Kafka:ContactProjectionProducer:BootstrapServers"] = "broker:9092",
                ["Kafka:ContactProjectionProducer:Topic"] = "contact-projection-topic"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddOptions();
        services.AddLogging();
        services.AddApiInfrastructureServices(configuration);

        using var serviceProvider = services.BuildServiceProvider();

        var projectionProducerOptions = serviceProvider.GetRequiredService<IOptions<ContactProjectionProducerSettingsSection>>().Value;
        var registry = serviceProvider.GetRequiredService<KafkaProducerSettingsRegistry>();
        var typedProjectionProducerOptions = registry.Get<ProjectionIntegrationEvent<ContactReadModel>>();

        projectionProducerOptions.BootstrapServers.Should().Be("broker:9092");
        projectionProducerOptions.Topic.Should().Be("contact-projection-topic");
        typedProjectionProducerOptions!.Topic.Should().Be("contact-projection-topic");
    }

    [Theory]
    [InlineData("SocialGraphService/src/FlowChat.SocialGraphService.API/appsettings.json")]
    [InlineData("SocialGraphService/src/FlowChat.SocialGraphService.API/appsettings.Development.json")]
    [InlineData("SocialGraphService/src/Workers/FlowChat.SocialGraphService.OutboxPublisher/appsettings.json")]
    [InlineData("SocialGraphService/src/Workers/FlowChat.SocialGraphService.OutboxPublisher/appsettings.Development.json")]
    public void AppSettingsFiles_ExposeRequiredKafkaProducerSections(string relativePath)
    {
        var configuration = new ConfigurationBuilder()
            .AddJsonFile(GetRepositoryPath(relativePath))
            .Build();

        var projectionProducerOptions = configuration
            .GetSection(new ContactProjectionProducerSettingsSection().SectionName)
            .Get<ContactProjectionProducerSettingsSection>();

        projectionProducerOptions.Should().NotBeNull();
        projectionProducerOptions!.Topic.Should().Be("dev.flowchat.social-graph.contact-projection.v1");
    }

    private static string GetRepositoryPath(string relativePath)
    {
        var currentDirectory = new DirectoryInfo(AppContext.BaseDirectory);

        while (currentDirectory is not null)
        {
            var candidatePath = Path.Combine(currentDirectory.FullName, relativePath);
            if (File.Exists(candidatePath))
            {
                return candidatePath;
            }

            currentDirectory = currentDirectory.Parent;
        }

        throw new InvalidOperationException($"Could not locate file '{relativePath}' starting from '{AppContext.BaseDirectory}'.");
    }
}
