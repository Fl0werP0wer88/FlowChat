using FlowChat.AuthService.Consumers;
using FlowChat.AuthService.Consumers.Kafka;
using FlowChat.AuthService.Consumers.Configuration.Settings;
using FlowChat.AuthService.Application.Contracts.Infrastructure;
using FlowChat.AuthService.Application.Contracts.Persistence;
using FlowChat.Shared.Application;
using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Messaging.Broker;

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
        services.AddConsumers(configuration);

        await using var serviceProvider = services.BuildServiceProvider();
        await using var scope = serviceProvider.CreateAsyncScope();

        var consumerCollection = serviceProvider.GetRequiredService<IConsumerCollection>();
        var authEmailChangedSubscriber = scope.ServiceProvider.GetRequiredService<AuthEmailChangedSubscriber>();
        var subscriber = scope.ServiceProvider.GetRequiredService<UserEmailConfirmedSubscriber>();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        var accountRepository = scope.ServiceProvider.GetRequiredService<IAccountRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var passwordHashingService = scope.ServiceProvider.GetRequiredService<IPasswordHashingService>();

        consumerCollection.Should().NotBeNull();
        authEmailChangedSubscriber.Should().NotBeNull();
        subscriber.Should().NotBeNull();
        mediator.Should().NotBeNull();
        accountRepository.Should().NotBeNull();
        unitOfWork.Should().NotBeNull();
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
        consumerOptions!.GroupId.Should().Be("auth-service");
        consumerOptions.RetryGroupId.Should().Be("auth-service-retry");
        consumerOptions.Topic.Should().Be("dev.flowchat.user-profile.user-profile.v1");
        consumerOptions.RetryTopic.Should().Be("dev.flowchat.user-profile.user-profile.v1.auth-service.retry");
        consumerOptions.DeadLetterTopic.Should().Be("dev.flowchat.user-profile.user-profile.v1.auth-service.dlq");
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
                ["Kafka:UserEmailConfirmedConsumer:RetryTopic"] = "dev.flowchat.user-profile.user-profile.v1.auth-service.retry",
                ["Kafka:UserEmailConfirmedConsumer:DeadLetterTopic"] = "dev.flowchat.user-profile.user-profile.v1.auth-service.dlq",
                ["Kafka:UserEmailConfirmedConsumer:MaxRetryCount"] = "5",
                ["Kafka:UserEmailConfirmedConsumer:RetryBaseDelaySeconds"] = "5",
                ["Kafka:UserEmailConfirmedConsumer:RetryMaxDelaySeconds"] = "300",
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
