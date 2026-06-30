using FlowChat.Shared.Application;
using FlowChat.UserProfileService.Application.Contracts.Infrastructure;
using FlowChat.UserProfileService.Application.Contracts.Persistence;
using FlowChat.UserProfileService.Consumers;
using FlowChat.UserProfileService.Consumers.Kafka;
using FlowChat.UserProfileService.Consumers.Configuration.Settings;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Messaging.Broker;

namespace FlowChat.UserProfileService.IntegrationTests;

public sealed class AccountRegisteredConsumerConfigurationTests
{
    [Fact]
    public async Task AddConsumers_RegistersMainAndRetryConsumers()
    {
        var configuration = CreateConfiguration();

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddOptions();
        services.AddLogging();
        services.AddConsumers(configuration);

        await using var serviceProvider = services.BuildServiceProvider();

        var consumerCollection = serviceProvider.GetRequiredService<IConsumerCollection>();
        var subscriber = serviceProvider.GetRequiredService<AccountRegisteredSubscriber>();

        consumerCollection.Should().NotBeNull();
        subscriber.Should().NotBeNull();
    }

    [Theory]
    [InlineData("UserProfileService/src/Workers/FlowChat.UserProfileService.Consumers/appsettings.json")]
    [InlineData("UserProfileService/src/Workers/FlowChat.UserProfileService.Consumers/appsettings.Development.json")]
    public void AppSettingsFiles_ExposeRequiredKafkaConsumerSection(string relativePath)
    {
        var configuration = new ConfigurationBuilder()
            .AddJsonFile(GetRepositoryPath(relativePath))
            .Build();

        var consumerOptions = configuration
            .GetSection(new AccountRegisteredConsumerSettingsSection().SectionName)
            .Get<AccountRegisteredConsumerSettingsSection>();

        consumerOptions.Should().NotBeNull();
        consumerOptions!.BootstrapServers.Should().Be("localhost:9092");
        consumerOptions.GroupId.Should().Be("userprofile-service");
        consumerOptions.RetryGroupId.Should().Be("userprofile-service-retry");
        consumerOptions.Topic.Should().Be("dev.flowchat.identity.user.v1");
        consumerOptions.RetryTopic.Should().Be("dev.flowchat.identity.user.v1.userprofile-service.retry");
        consumerOptions.DeadLetterTopic.Should().Be("dev.flowchat.identity.user.v1.userprofile-service.dlq");

        configuration.GetConnectionString("UserProfileDb").Should().Be(
            "Host=localhost;Port=5432;Database=flowchat_userprofile_db;Username=flowchat_app;Password=flowchat_app_pw;");
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

        using var scope = serviceProvider.CreateScope();

        scope.ServiceProvider.GetRequiredService<IMediator>().Should().NotBeNull();
        scope.ServiceProvider.GetRequiredService<IUserProfileReadRepository>().Should().NotBeNull();
        scope.ServiceProvider.GetRequiredService<IUserProfileWriteRepository>().Should().NotBeNull();
        scope.ServiceProvider.GetRequiredService<IEmailVerificationProcessWriteRepository>().Should().NotBeNull();
        scope.ServiceProvider.GetRequiredService<IUnitOfWork>().Should().BeAssignableTo<IConsumedOffsetCommitter>();
        scope.ServiceProvider.GetRequiredService<IEmailVerificationLinkBuilder>().Should().NotBeNull();
        scope.ServiceProvider.GetRequiredService<IEmailVerificationTokenProtector>().Should().NotBeNull();
    }

    private static IConfiguration CreateConfiguration() =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:UserProfileDb"] = "Host=localhost;Database=test",
                ["Kafka:AccountRegisteredConsumer:BootstrapServers"] = "localhost:9092",
                ["Kafka:AccountRegisteredConsumer:GroupId"] = "userprofile-service",
                ["Kafka:AccountRegisteredConsumer:RetryGroupId"] = "userprofile-service-retry",
                ["Kafka:AccountRegisteredConsumer:Topic"] = "dev.flowchat.identity.user.v1",
                ["Kafka:AccountRegisteredConsumer:RetryTopic"] = "dev.flowchat.identity.user.v1.userprofile-service.retry",
                ["Kafka:AccountRegisteredConsumer:DeadLetterTopic"] = "dev.flowchat.identity.user.v1.userprofile-service.dlq",
                ["Kafka:AccountRegisteredConsumer:MaxRetryCount"] = "5",
                ["Kafka:AccountRegisteredConsumer:RetryBaseDelaySeconds"] = "5",
                ["Kafka:AccountRegisteredConsumer:RetryMaxDelaySeconds"] = "300",
                ["Kafka:AccountRegisteredConsumer:AutoOffsetReset"] = "Earliest",
                ["Kafka:UserEmailConfirmedProducer:BootstrapServers"] = "localhost:9092",
                ["Kafka:UserEmailConfirmedProducer:Topic"] = "test.user-email-confirmed",
                ["Kafka:UserEmailVerificationRequestedProducer:BootstrapServers"] = "localhost:9092",
                ["Kafka:UserEmailVerificationRequestedProducer:Topic"] = "test.email-verification",
                ["Kafka:UserProfileProjectionProducer:BootstrapServers"] = "localhost:9092",
                ["Kafka:UserProfileProjectionProducer:Topic"] = "test.user-profile-projection",
                ["ConfirmationLinks:EmailVerificationBaseUrl"] = "https://test.example.com/verify",
                ["FlowChat:ApiUrl"] = "https://localhost"
            })
            .Build();
}
