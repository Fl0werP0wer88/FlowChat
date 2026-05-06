using FlowChat.Core.Contracts;
using FlowChat.Core.Messaging.RealtimeService.Events;
using FlowChat.RealtimeService.Infrastructure;
using FlowChat.RealtimeService.Infrastructure.Configuration.Settings;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FlowChat.RealtimeService.UnitTests;

public sealed class KafkaProducerConfigurationTests
{
    [Fact]
    public void AddInfrastructureServices_ResolvesDedicatedKafkaProducerOptions()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Redis"] = "localhost:6379,password=secret",
                ["RealtimeConnections:InstanceId"] = "realtime-instance",
                ["RealtimeConnections:KeyPrefix"] = "flowchat:realtime",
                ["RealtimeConnections:ConnectionTtl"] = "00:01:00",
                ["RealtimeConnections:RefreshInterval"] = "00:00:30",
                ["PresenceService:BaseUrl"] = "http://localhost:5098",
                ["PresenceService:InternalApiKey"] = "internal-key",
                ["Kafka:RealtimeConnectionUnregisteredProducer:BootstrapServers"] = "broker:9092",
                ["Kafka:RealtimeConnectionUnregisteredProducer:Topic"] = "unregistered-topic"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddOptions();
        services.AddLogging();
        services.AddInfrastructureServices(configuration);

        using var serviceProvider = services.BuildServiceProvider();

        var settingsProvider = serviceProvider.GetRequiredService<ISettingsProvider>();
        var unregisteredOptions = settingsProvider.GetSection<RealtimeConnectionUnregisteredProducerSettingsSection>();
        var typedUnregisteredOptions = serviceProvider
            .GetRequiredService<IKafkaProducerSettingsSection<RealtimeConnectionUnregisteredIntegrationEvent>>();

        unregisteredOptions.BootstrapServers.Should().Be("broker:9092");
        unregisteredOptions.Topic.Should().Be("unregistered-topic");
        typedUnregisteredOptions.Topic.Should().Be("unregistered-topic");
    }

    [Theory]
    [InlineData("RealtimeService/src/FlowChat.RealtimeService.API/appsettings.json")]
    [InlineData("RealtimeService/src/FlowChat.RealtimeService.API/appsettings.Development.json")]
    public void AppSettingsFiles_ExposeRequiredKafkaProducerSections(string relativePath)
    {
        var configuration = new ConfigurationBuilder()
            .AddJsonFile(GetRepositoryPath(relativePath))
            .Build();

        var unregisteredOptions = configuration
            .GetSection(new RealtimeConnectionUnregisteredProducerSettingsSection().SectionName)
            .Get<RealtimeConnectionUnregisteredProducerSettingsSection>();

        unregisteredOptions.Should().NotBeNull();
        unregisteredOptions!.BootstrapServers.Should().Be("localhost:9092");
        unregisteredOptions.Topic.Should().Be("dev.flowchat.realtime.connection.v1");
    }

    [Fact]
    public void DevelopmentSettings_PresenceInternalApiKeyMatchesPresenceService()
    {
        var realtimeConfiguration = new ConfigurationBuilder()
            .AddJsonFile(GetRepositoryPath("RealtimeService/src/FlowChat.RealtimeService.API/appsettings.json"))
            .AddJsonFile(GetRepositoryPath("RealtimeService/src/FlowChat.RealtimeService.API/appsettings.Development.json"))
            .Build();
        var presenceConfiguration = new ConfigurationBuilder()
            .AddJsonFile(GetRepositoryPath("PresenceService/src/FlowChat.PresenceService.API/appsettings.json"))
            .AddJsonFile(GetRepositoryPath("PresenceService/src/FlowChat.PresenceService.API/appsettings.Development.json"))
            .Build();

        realtimeConfiguration["PresenceService:InternalApiKey"]
            .Should().Be(presenceConfiguration["FlowChat:InternalApi:ApiKey"]);
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
