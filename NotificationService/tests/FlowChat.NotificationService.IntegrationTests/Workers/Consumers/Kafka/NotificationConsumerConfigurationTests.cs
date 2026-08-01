using FlowChat.NotificationService.Application.Contracts.Infrastructure;
using FlowChat.NotificationService.Application.Contracts.Persistence;
using FlowChat.NotificationService.Consumers;
using FlowChat.NotificationService.Consumers.Kafka;
using FlowChat.NotificationService.Consumers.Configuration.Settings;
using FlowChat.NotificationService.Persistence;
using FlowChat.Shared.Application;
using FlowChat.Shared.Infrastructure.Silverback.Kafka.Retry;
using FluentAssertions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Moq;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Publishing;

namespace FlowChat.NotificationService.IntegrationTests.Workers.Consumers.Kafka;

public sealed class NotificationConsumerConfigurationTests
{
    [Fact]
    public async Task AddConsumers_RegistersTieredRetryConsumersAndProducers()
    {
        var configuration = CreateConfiguration();

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddOptions();
        services.AddLogging();
        services.AddSingleton(Mock.Of<IHostApplicationLifetime>());
        services.AddConsumers(configuration);

        await using var serviceProvider = services.BuildServiceProvider();
        await using var scope = serviceProvider.CreateAsyncScope();

        await serviceProvider.GetRequiredService<IBrokerClientsConnector>().InitializeAsync();
        var consumerCollection = serviceProvider.GetRequiredService<IConsumerCollection>();
        var producerCollection = serviceProvider.GetRequiredService<IProducerCollection>();
        var topology = serviceProvider.GetRequiredService<TieredKafkaRetryTopology>();
        var subscriber = scope.ServiceProvider.GetRequiredService<UserEmailVerificationRequestedSubscriber>();

        consumerCollection.Should().HaveCount(5);
        producerCollection.Should().HaveCount(5);
        topology.Streams.Should().ContainSingle();
        topology.Streams[0].RetryTiers
            .Select(tier => tier.Topic)
            .Append(topology.Streams[0].DeadLetterTopic)
            .Should()
            .AllSatisfy(topic => producerCollection.GetProducerForEndpoint(topic).Should().NotBeNull());
        subscriber.Should().NotBeNull();
    }

    [Theory]
    [InlineData("NotificationService/src/Workers/FlowChat.NotificationService.Consumers/appsettings.json")]
    [InlineData("NotificationService/src/Workers/FlowChat.NotificationService.Consumers/appsettings.Development.json")]
    public void AppSettingsFiles_ExposeRequiredKafkaConsumerSection(string relativePath)
    {
        var configuration = new ConfigurationBuilder()
            .AddJsonFile(GetRepositoryPath(relativePath))
            .Build();

        var consumerOptions = configuration
            .GetSection(new UserEmailVerificationRequestedConsumerSettingsSection().SectionName)
            .Get<UserEmailVerificationRequestedConsumerSettingsSection>();

        consumerOptions.Should().NotBeNull();
        consumerOptions!.BootstrapServers.Should().Be("localhost:9092");
        consumerOptions.GroupId.Should().Be("notification-service");
        consumerOptions.RetryGroupId.Should().Be("notification-service-retry");
        consumerOptions.Topic.Should().Be("dev.flowchat.notification.email.v1");
        consumerOptions.DeadLetterTopic.Should().Be("dev.flowchat.notification.email.v1.notification-service.dlq");
        consumerOptions.RetryTiers.Select(tier => tier.Topic).Should().Equal(
            "dev.flowchat.notification.email.v1.notification-service.retry",
            "dev.flowchat.notification.email.v1.notification-service.retry.20s",
            "dev.flowchat.notification.email.v1.notification-service.retry.60s",
            "dev.flowchat.notification.email.v1.notification-service.retry.300s");
        consumerOptions.RetryTiers.Select(tier => tier.Delay).Should().Equal(
            TimeSpan.FromSeconds(5),
            TimeSpan.FromSeconds(20),
            TimeSpan.FromSeconds(60),
            TimeSpan.FromSeconds(300));
        configuration.GetConnectionString("NotificationDb").Should().Be(
            "Host=localhost;Port=5432;Database=flowchat_notification_db;Username=flowchat_app;Password=flowchat_app_pw;");
    }

    [Fact]
    public async Task AddConsumers_RegistersCommandHandlerDependencies()
    {
        var configuration = CreateConfiguration();

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddOptions();
        services.AddLogging();
        services.AddSingleton(Mock.Of<IHostApplicationLifetime>());
        services.AddConsumers(configuration);

        await using var serviceProvider = services.BuildServiceProvider();
        await using var scope = serviceProvider.CreateAsyncScope();

        scope.ServiceProvider.GetRequiredService<IMediator>().Should().NotBeNull();
        scope.ServiceProvider.GetRequiredService<IDbContextFactory<AppDbContext>>().Should().NotBeNull();
        scope.ServiceProvider.GetRequiredService<INotificationWriteRepository>().Should().NotBeNull();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var offsetCommitter = scope.ServiceProvider.GetRequiredService<IConsumedOffsetCommitter>();
        unitOfWork.Should().BeSameAs(offsetCommitter);
        scope.ServiceProvider.GetRequiredService<INotificationSender>().Should().NotBeNull();
    }

    private static IConfiguration CreateConfiguration()
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:NotificationDb"] = "Host=localhost;Database=test",
                ["EmailSettings:SmtpHost"] = "localhost",
                ["EmailSettings:SmtpPort"] = "1025",
                ["EmailSettings:EnableSsl"] = "false",
                ["EmailSettings:FromEmail"] = "noreply@flowchat.local",
                ["EmailSettings:FromName"] = "FlowChat Notifications",
                ["Kafka:UserEmailVerificationRequestedConsumer:BootstrapServers"] = "localhost:9092",
                ["Kafka:UserEmailVerificationRequestedConsumer:GroupId"] = "notification-service",
                ["Kafka:UserEmailVerificationRequestedConsumer:RetryGroupId"] = "notification-service-retry",
                ["Kafka:UserEmailVerificationRequestedConsumer:Topic"] = "dev.flowchat.notification.email.v1",
                ["Kafka:UserEmailVerificationRequestedConsumer:DeadLetterTopic"] = "dev.flowchat.notification.email.v1.notification-service.dlq",
                ["Kafka:UserEmailVerificationRequestedConsumer:RetryTiers:0:Topic"] = "dev.flowchat.notification.email.v1.notification-service.retry",
                ["Kafka:UserEmailVerificationRequestedConsumer:RetryTiers:0:Delay"] = "00:00:05",
                ["Kafka:UserEmailVerificationRequestedConsumer:RetryTiers:1:Topic"] = "dev.flowchat.notification.email.v1.notification-service.retry.20s",
                ["Kafka:UserEmailVerificationRequestedConsumer:RetryTiers:1:Delay"] = "00:00:20",
                ["Kafka:UserEmailVerificationRequestedConsumer:RetryTiers:2:Topic"] = "dev.flowchat.notification.email.v1.notification-service.retry.60s",
                ["Kafka:UserEmailVerificationRequestedConsumer:RetryTiers:2:Delay"] = "00:01:00",
                ["Kafka:UserEmailVerificationRequestedConsumer:RetryTiers:3:Topic"] = "dev.flowchat.notification.email.v1.notification-service.retry.300s",
                ["Kafka:UserEmailVerificationRequestedConsumer:RetryTiers:3:Delay"] = "00:05:00",
                ["Kafka:UserEmailVerificationRequestedConsumer:AutoOffsetReset"] = "Earliest"
            })
            .Build();
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
