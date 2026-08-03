using FlowChat.Shared.Application;
using FlowChat.Shared.Infrastructure.Silverback.Kafka.Retry;
using FlowChat.Shared.Infrastructure.Silverback.Persistence;
using FlowChat.UserProfileService.Application.Contracts.Infrastructure;
using FlowChat.UserProfileService.Application.Contracts.Persistence;
using FlowChat.UserProfileService.Consumers;
using FlowChat.UserProfileService.Consumers.Kafka;
using FlowChat.UserProfileService.Consumers.Configuration.Settings;
using FlowChat.UserProfileService.Persistence;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Moq;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Publishing;

namespace FlowChat.UserProfileService.IntegrationTests;

public sealed class AccountRegisteredConsumerConfigurationTests
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

        await serviceProvider.GetRequiredService<IBrokerClientsConnector>().InitializeAsync();
        var consumerCollection = serviceProvider.GetRequiredService<IConsumerCollection>();
        var producerCollection = serviceProvider.GetRequiredService<IProducerCollection>();
        var topology = serviceProvider.GetRequiredService<TieredKafkaRetryTopology>();
        var subscriber = serviceProvider.GetRequiredService<AccountRegisteredSubscriber>();

        consumerCollection.Should().HaveCount(5);
        topology.Streams.Should().ContainSingle();
        topology.Streams[0].RetryTiers
            .Select(tier => tier.Topic)
            .Append(topology.Streams[0].DeadLetterTopic)
            .Should()
            .AllSatisfy(topic => producerCollection.GetProducerForEndpoint(topic).Should().NotBeNull());
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
        consumerOptions.RetryTiers.Select(tier => tier.Topic).Should().Equal(
            "dev.flowchat.identity.user.v1.userprofile-service.retry.5s",
            "dev.flowchat.identity.user.v1.userprofile-service.retry.20s",
            "dev.flowchat.identity.user.v1.userprofile-service.retry.60s",
            "dev.flowchat.identity.user.v1.userprofile-service.retry.300s");
        consumerOptions.RetryTiers.Select(tier => tier.Delay).Should().Equal(
            TimeSpan.FromSeconds(5),
            TimeSpan.FromSeconds(20),
            TimeSpan.FromSeconds(60),
            TimeSpan.FromSeconds(300));
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
        services.AddSingleton(Mock.Of<IHostApplicationLifetime>());
        services.AddConsumers(configuration);

        await using var serviceProvider = services.BuildServiceProvider();

        using var scope = serviceProvider.CreateScope();

        scope.ServiceProvider.GetRequiredService<IMediator>().Should().NotBeNull();
        scope.ServiceProvider.GetRequiredService<IUserProfileReadRepository>().Should().NotBeNull();
        scope.ServiceProvider.GetRequiredService<IUserProfileWriteRepository>().Should().NotBeNull();
        scope.ServiceProvider.GetRequiredService<IEmailVerificationProcessWriteRepository>().Should().NotBeNull();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var offsetCommitter = scope.ServiceProvider.GetRequiredService<IConsumedOffsetCommitter>();
        unitOfWork.Should().BeSameAs(offsetCommitter)
            .And.BeOfType<SilverbackKafkaOffsetUnitOfWork<AppDbContext>>();
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
                ["Kafka:AccountRegisteredConsumer:DeadLetterTopic"] = "dev.flowchat.identity.user.v1.userprofile-service.dlq",
                ["Kafka:AccountRegisteredConsumer:RetryTiers:0:Topic"] = "dev.flowchat.identity.user.v1.userprofile-service.retry.5s",
                ["Kafka:AccountRegisteredConsumer:RetryTiers:0:Delay"] = "00:00:05",
                ["Kafka:AccountRegisteredConsumer:RetryTiers:1:Topic"] = "dev.flowchat.identity.user.v1.userprofile-service.retry.20s",
                ["Kafka:AccountRegisteredConsumer:RetryTiers:1:Delay"] = "00:00:20",
                ["Kafka:AccountRegisteredConsumer:RetryTiers:2:Topic"] = "dev.flowchat.identity.user.v1.userprofile-service.retry.60s",
                ["Kafka:AccountRegisteredConsumer:RetryTiers:2:Delay"] = "00:01:00",
                ["Kafka:AccountRegisteredConsumer:RetryTiers:3:Topic"] = "dev.flowchat.identity.user.v1.userprofile-service.retry.300s",
                ["Kafka:AccountRegisteredConsumer:RetryTiers:3:Delay"] = "00:05:00",
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
