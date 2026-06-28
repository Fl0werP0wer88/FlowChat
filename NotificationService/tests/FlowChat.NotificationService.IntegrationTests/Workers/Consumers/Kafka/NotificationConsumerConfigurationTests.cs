using FlowChat.NotificationService.Application.Contracts.Infrastructure;
using FlowChat.NotificationService.Application.Contracts.Persistence;
using FlowChat.NotificationService.Consumers;
using FlowChat.NotificationService.Consumers.Kafka;
using FlowChat.NotificationService.Consumers.Configuration.Settings;
using FlowChat.Shared.Application;
using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Messaging.Broker;

namespace FlowChat.NotificationService.UnitTests;

public sealed class NotificationConsumerConfigurationTests
{
    [Fact]
    public async Task AddConsumers_RegistersConsumerInfrastructure()
    {
        var configuration = CreateConfiguration();

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddOptions();
        services.AddLogging();
        services.AddConsumers(configuration);

        await using var serviceProvider = services.BuildServiceProvider();
        await using var scope = serviceProvider.CreateAsyncScope();

        var consumerCollection = serviceProvider.GetRequiredService<IConsumerCollection>();
        var subscriber = scope.ServiceProvider.GetRequiredService<UserEmailVerificationRequestedSubscriber>();

        consumerCollection.Should().NotBeNull();
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
        consumerOptions!.GroupId.Should().Be("notification-service");
        consumerOptions.RetryGroupId.Should().Be("notification-service-retry");
        consumerOptions.Topic.Should().Be("dev.flowchat.notification.email.v1");
        consumerOptions.RetryTopic.Should().Be("dev.flowchat.notification.email.v1.notification-service.retry");
        consumerOptions.DeadLetterTopic.Should().Be("dev.flowchat.notification.email.v1.notification-service.dlq");
    }

    [Fact]
    public async Task AddConsumers_RegistersCommandHandlerDependencies()
    {
        var configuration = CreateConfiguration();

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddOptions();
        services.AddLogging();
        services.AddConsumers(configuration);

        await using var serviceProvider = services.BuildServiceProvider();
        await using var scope = serviceProvider.CreateAsyncScope();

        scope.ServiceProvider.GetRequiredService<IMediator>().Should().NotBeNull();
        scope.ServiceProvider.GetRequiredService<INotificationWriteRepository>().Should().NotBeNull();
        scope.ServiceProvider.GetRequiredService<IUnitOfWork>().Should().NotBeNull();
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
                ["Kafka:UserEmailVerificationRequestedConsumer:RetryTopic"] = "dev.flowchat.notification.email.v1.notification-service.retry",
                ["Kafka:UserEmailVerificationRequestedConsumer:DeadLetterTopic"] = "dev.flowchat.notification.email.v1.notification-service.dlq",
                ["Kafka:UserEmailVerificationRequestedConsumer:MaxRetryCount"] = "5",
                ["Kafka:UserEmailVerificationRequestedConsumer:RetryBaseDelaySeconds"] = "5",
                ["Kafka:UserEmailVerificationRequestedConsumer:RetryMaxDelaySeconds"] = "300",
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
