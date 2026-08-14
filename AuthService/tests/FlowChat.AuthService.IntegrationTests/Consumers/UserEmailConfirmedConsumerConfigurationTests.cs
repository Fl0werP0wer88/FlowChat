using FlowChat.AuthService.Consumers;
using FlowChat.AuthService.Consumers.Kafka;
using FlowChat.AuthService.Consumers.Configuration.Settings;
using FlowChat.AuthService.Application.Contracts.Infrastructure;
using FlowChat.AuthService.Application.Contracts.Persistence;
using FlowChat.AuthService.Persistence;
using FlowChat.Shared.Application;
using FlowChat.Shared.Infrastructure.Silverback.Kafka.Retry;
using FlowChat.Shared.Infrastructure.Silverback.Persistence;
using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Moq;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Publishing;

namespace FlowChat.AuthService.UnitTests;

public sealed class UserEmailConfirmedConsumerConfigurationTests
{
    [Fact]
    public async Task AddConsumers_RegistersConsumerInfrastructure()
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
        await using var scope = serviceProvider.CreateAsyncScope();

        var consumerCollection = serviceProvider.GetRequiredService<IConsumerCollection>();
        var producerCollection = serviceProvider.GetRequiredService<IProducerCollection>();
        var topology = serviceProvider.GetRequiredService<TieredKafkaRetryTopology>();
        var authEmailChangedSubscriber = scope.ServiceProvider.GetRequiredService<AuthEmailChangedSubscriber>();
        var subscriber = scope.ServiceProvider.GetRequiredService<UserEmailConfirmedSubscriber>();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        var accountRepository = scope.ServiceProvider.GetRequiredService<IAccountRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var offsetCommitter = scope.ServiceProvider.GetRequiredService<IConsumedOffsetCommitter>();
        var passwordHashingService = scope.ServiceProvider.GetRequiredService<IPasswordHashingService>();

        consumerCollection.Should().HaveCount(5);
        topology.Streams.Should().ContainSingle();
        topology.Streams[0].RetryTiers
            .Select(tier => tier.Topic)
            .Append(topology.Streams[0].DeadLetterTopic)
            .Should()
            .AllSatisfy(topic => producerCollection.GetProducerForEndpoint(topic).Should().NotBeNull());
        producerCollection.GetProducerForEndpoint("auth-account-confirmed").Should().NotBeNull();
        authEmailChangedSubscriber.Should().NotBeNull();
        subscriber.Should().NotBeNull();
        mediator.Should().NotBeNull();
        accountRepository.Should().NotBeNull();
        unitOfWork.Should().BeSameAs(offsetCommitter)
            .And.BeOfType<SilverbackKafkaOffsetUnitOfWork<AppDbContext>>();
        passwordHashingService.Should().NotBeNull();
    }

    [Theory]
    [InlineData("AuthService/src/Workers/FlowChat.AuthService.Consumers/appsettings.json")]
    [InlineData("AuthService/src/Workers/FlowChat.AuthService.Consumers/appsettings.Development.json")]
    public void AppSettingsFiles_ExposeRequiredKafkaConsumerSection(string relativePath)
    {
        var configuration = new ConfigurationBuilder()
            .AddJsonFile(GetRepositoryPath(relativePath))
            .Build();

        var consumerOptions = configuration
            .GetSection(new UserEmailConfirmedConsumerSettingsSection().SectionName)
            .Get<UserEmailConfirmedConsumerSettingsSection>();

        consumerOptions.Should().NotBeNull();
        consumerOptions!.BootstrapServers.Should().Be("localhost:9092");
        consumerOptions.GroupId.Should().Be("auth-service");
        consumerOptions.RetryGroupId.Should().Be("auth-service-retry");
        consumerOptions.Topic.Should().Be("dev.flowchat.user-profile.user-profile.v1");
        consumerOptions.RetryTiers.Select(tier => tier.Topic).Should().Equal(
            "dev.flowchat.user-profile.user-profile.v1.auth-service.retry.5s",
            "dev.flowchat.user-profile.user-profile.v1.auth-service.retry.20s",
            "dev.flowchat.user-profile.user-profile.v1.auth-service.retry.60s",
            "dev.flowchat.user-profile.user-profile.v1.auth-service.retry.300s");
        consumerOptions.RetryTiers.Select(tier => tier.Delay).Should().Equal(
            TimeSpan.FromSeconds(5),
            TimeSpan.FromSeconds(20),
            TimeSpan.FromSeconds(60),
            TimeSpan.FromSeconds(300));
        consumerOptions.DeadLetterTopic.Should().Be("dev.flowchat.user-profile.user-profile.v1.auth-service.dlq");
        configuration.GetConnectionString("AuthDb").Should().Be(
            "Host=localhost;Port=5432;Database=flowchat_auth_db;Username=flowchat_app;Password=flowchat_app_pw;");
    }

    private static IConfiguration CreateConfiguration()
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:AuthDb"] = "Host=localhost;Database=auth-test",
                ["Kafka:UserEmailConfirmedConsumer:BootstrapServers"] = "localhost:9092",
                ["Kafka:UserEmailConfirmedConsumer:GroupId"] = "auth-service",
                ["Kafka:UserEmailConfirmedConsumer:RetryGroupId"] = "auth-service-retry",
                ["Kafka:UserEmailConfirmedConsumer:Topic"] = "dev.flowchat.user-profile.user-profile.v1",
                ["Kafka:UserEmailConfirmedConsumer:DeadLetterTopic"] = "dev.flowchat.user-profile.user-profile.v1.auth-service.dlq",
                ["Kafka:UserEmailConfirmedConsumer:RetryTiers:0:Topic"] = "dev.flowchat.user-profile.user-profile.v1.auth-service.retry.5s",
                ["Kafka:UserEmailConfirmedConsumer:RetryTiers:0:Delay"] = "00:00:05",
                ["Kafka:UserEmailConfirmedConsumer:RetryTiers:1:Topic"] = "dev.flowchat.user-profile.user-profile.v1.auth-service.retry.20s",
                ["Kafka:UserEmailConfirmedConsumer:RetryTiers:1:Delay"] = "00:00:20",
                ["Kafka:UserEmailConfirmedConsumer:RetryTiers:2:Topic"] = "dev.flowchat.user-profile.user-profile.v1.auth-service.retry.60s",
                ["Kafka:UserEmailConfirmedConsumer:RetryTiers:2:Delay"] = "00:01:00",
                ["Kafka:UserEmailConfirmedConsumer:RetryTiers:3:Topic"] = "dev.flowchat.user-profile.user-profile.v1.auth-service.retry.300s",
                ["Kafka:UserEmailConfirmedConsumer:RetryTiers:3:Delay"] = "00:05:00",
                ["Kafka:UserEmailConfirmedConsumer:AutoOffsetReset"] = "Earliest"
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
