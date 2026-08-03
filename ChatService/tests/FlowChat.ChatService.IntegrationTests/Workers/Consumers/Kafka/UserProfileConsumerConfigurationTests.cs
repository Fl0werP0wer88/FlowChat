using FlowChat.ChatService.Application.Features.UserProfile;
using FlowChat.ChatService.Consumers;
using FlowChat.ChatService.Consumers.Configuration.Settings;
using FlowChat.ChatService.Persistence;
using FlowChat.Shared.Application;
using FlowChat.Shared.Infrastructure.Silverback.Kafka.Retry;
using FlowChat.Shared.Infrastructure.Silverback.Persistence;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Moq;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Publishing;

namespace FlowChat.ChatService.IntegrationTests.Workers.Consumers.Kafka;

public sealed class UserProfileConsumerConfigurationTests
{
    [Fact]
    public async Task AddConsumers_RegistersSingleProjectionTieredRetryPipeline()
    {
        var configuration = CreateConfiguration();
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddOptions();
        services.AddLogging();
        services.AddSingleton(Mock.Of<IHostApplicationLifetime>());
        services.AddConsumers(configuration);

        await using var provider = services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateOnBuild = true,
            ValidateScopes = true
        });
        await provider.GetRequiredService<IBrokerClientsConnector>().InitializeAsync();
        await using var scope = provider.CreateAsyncScope();

        var consumers = provider.GetRequiredService<IConsumerCollection>();
        var producers = provider.GetRequiredService<IProducerCollection>();
        var topology = provider.GetRequiredService<TieredKafkaRetryTopology>();
        var repository = scope.ServiceProvider.GetRequiredService<IProjectionSingleRepository<UserProfileProjectionDto>>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var offsetCommitter = scope.ServiceProvider.GetRequiredService<IConsumedOffsetCommitter>();

        consumers.Should().HaveCount(5);
        topology.Streams.Should().ContainSingle();
        topology.Streams[0].RetryTiers
            .Select(tier => tier.Topic)
            .Append(topology.Streams[0].DeadLetterTopic)
            .Should()
            .AllSatisfy(topic => producers.GetProducerForEndpoint(topic).Should().NotBeNull());
        repository.Should().NotBeNull();
        unitOfWork.Should().BeSameAs(offsetCommitter)
            .And.BeOfType<SilverbackKafkaOffsetUnitOfWork<AppDbContext>>();
    }

    [Theory]
    [InlineData("ChatService/src/Workers/FlowChat.ChatService.Consumers/appsettings.json")]
    [InlineData("ChatService/src/Workers/FlowChat.ChatService.Consumers/appsettings.Development.json")]
    public void AppSettingsFiles_ExposeTieredRetrySettings(string relativePath)
    {
        var configuration = new ConfigurationBuilder()
            .AddJsonFile(GetRepositoryPath(relativePath))
            .Build();
        var settings = configuration
            .GetSection(new UserProfileConsumerSettingsSection().SectionName)
            .Get<UserProfileConsumerSettingsSection>();

        settings.Should().NotBeNull();
        settings!.GroupId.Should().Be("chat-service");
        settings.RetryGroupId.Should().Be("chat-service-retry");
        settings.Topic.Should().Be("dev.flowchat.user-profile.user-profile-projection.v1");
        settings.RetryTiers.Select(tier => tier.Topic).Should().Equal(
            "dev.flowchat.user-profile.user-profile-projection.v1.chat-service.retry",
            "dev.flowchat.user-profile.user-profile-projection.v1.chat-service.retry.20s",
            "dev.flowchat.user-profile.user-profile-projection.v1.chat-service.retry.60s",
            "dev.flowchat.user-profile.user-profile-projection.v1.chat-service.retry.300s");
        settings.RetryTiers.Select(tier => tier.Delay).Should().Equal(
            TimeSpan.FromSeconds(5),
            TimeSpan.FromSeconds(20),
            TimeSpan.FromSeconds(60),
            TimeSpan.FromSeconds(300));
        settings.DeadLetterTopic.Should().Be("dev.flowchat.user-profile.user-profile-projection.v1.chat-service.dlq");
    }

    private static IConfiguration CreateConfiguration() => new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:ChatDb"] = "Host=localhost;Database=chat-test",
            ["Kafka:UserProfileConsumer:BootstrapServers"] = "localhost:9092",
            ["Kafka:UserProfileConsumer:GroupId"] = "chat-service",
            ["Kafka:UserProfileConsumer:RetryGroupId"] = "chat-service-retry",
            ["Kafka:UserProfileConsumer:Topic"] = "dev.flowchat.user-profile.user-profile-projection.v1",
            ["Kafka:UserProfileConsumer:DeadLetterTopic"] = "dev.flowchat.user-profile.user-profile-projection.v1.chat-service.dlq",
            ["Kafka:UserProfileConsumer:RetryTiers:0:Topic"] = "dev.flowchat.user-profile.user-profile-projection.v1.chat-service.retry",
            ["Kafka:UserProfileConsumer:RetryTiers:0:Delay"] = "00:00:05",
            ["Kafka:UserProfileConsumer:RetryTiers:1:Topic"] = "dev.flowchat.user-profile.user-profile-projection.v1.chat-service.retry.20s",
            ["Kafka:UserProfileConsumer:RetryTiers:1:Delay"] = "00:00:20",
            ["Kafka:UserProfileConsumer:RetryTiers:2:Topic"] = "dev.flowchat.user-profile.user-profile-projection.v1.chat-service.retry.60s",
            ["Kafka:UserProfileConsumer:RetryTiers:2:Delay"] = "00:01:00",
            ["Kafka:UserProfileConsumer:RetryTiers:3:Topic"] = "dev.flowchat.user-profile.user-profile-projection.v1.chat-service.retry.300s",
            ["Kafka:UserProfileConsumer:RetryTiers:3:Delay"] = "00:05:00",
            ["Kafka:UserProfileConsumer:AutoOffsetReset"] = "Earliest"
        })
        .Build();

    private static string GetRepositoryPath(string relativePath)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            var candidate = Path.Combine(directory.FullName, relativePath);
            if (File.Exists(candidate))
                return candidate;

            directory = directory.Parent;
        }

        throw new InvalidOperationException($"Could not locate file '{relativePath}'.");
    }
}
