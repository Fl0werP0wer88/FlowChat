using Confluent.Kafka;
using FlowChat.PresenceService.Application.Features.ContactObserverProjections;
using FlowChat.PresenceService.Consumers;
using FlowChat.PresenceService.Consumers.Configuration.Settings;
using FlowChat.PresenceService.Persistence.Entities;
using FlowChat.Shared.Application;
using FlowChat.Shared.Infrastructure.Silverback.Kafka;
using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Configuration.Kafka;

namespace FlowChat.PresenceService.UnitTests.Workers.Consumers.Kafka;

public sealed class DuetConversationContactConsumerConfigurationTests
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
        var bulkRepository = scope.ServiceProvider
            .GetRequiredService<IProjectionBulkRepository<ProjectionCommandItem<ContactObserverProjectionDto>>>();
        var commandHandler = scope.ServiceProvider
            .GetRequiredService<IRequestHandler<ProjectionBulkCommand<ProjectionCommandItem<ContactObserverProjectionDto>>, FlowChat.Core.Results.FlowChatResult<Unit>>>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        consumerCollection.Should().NotBeNull();
        bulkRepository.Should().NotBeNull();
        commandHandler.Should().NotBeNull();
        unitOfWork.Should().BeAssignableTo<IConsumedOffsetCommitter>();
    }

    [Fact]
    public async Task AddConsumers_ConfiguresBatchProcessingOnlyForMainConsumer()
    {
        var options = CreateConfiguration()
            .GetSection(new DuetConversationContactConsumerSettingsSection().SectionName)
            .Get<DuetConversationContactConsumerSettingsSection>()!;

        var mainEndpoint = await GetEndpointConfigurationAsync(endpoint => endpoint
            .ConfigureFlowChatMainEndpoint(options)
            .EnableBatchProcessing(
                options.BatchSize,
                TimeSpan.FromMilliseconds(options.BatchMaxWaitTimeMilliseconds)));
        var retryEndpoint = await GetEndpointConfigurationAsync(endpoint =>
            endpoint.ConfigureFlowChatRetryEndpoint(options));

        mainEndpoint.Batch.Should().NotBeNull();
        retryEndpoint.Batch.Should().BeNull();
    }

    [Theory]
    [InlineData("PresenceService/src/Workers/FlowChat.PresenceService.Consumers/appsettings.json")]
    [InlineData("PresenceService/src/Workers/FlowChat.PresenceService.Consumers/appsettings.Development.json")]
    public void AppSettingsFiles_ExposeRequiredKafkaConsumerSection(string relativePath)
    {
        var configuration = new ConfigurationBuilder()
            .AddJsonFile(GetRepositoryPath(relativePath))
            .Build();

        var consumerOptions = configuration
            .GetSection(new DuetConversationContactConsumerSettingsSection().SectionName)
            .Get<DuetConversationContactConsumerSettingsSection>();

        consumerOptions.Should().NotBeNull();
        consumerOptions!.GroupId.Should().Be("presence-service");
        consumerOptions.RetryGroupId.Should().Be("presence-service-duet-conversation-contact-retry");
        consumerOptions.Topic.Should().Be("dev.flowchat.chat.duet-conversation-projection.v1");
        consumerOptions.RetryTopic.Should().Be("dev.flowchat.chat.duet-conversation-projection.v1.presence-service.retry");
        consumerOptions.DeadLetterTopic.Should().Be("dev.flowchat.chat.duet-conversation-projection.v1.presence-service.dlq");
    }

    private static IConfiguration CreateConfiguration()
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:PresenceDb"] = "Host=localhost;Database=flowchat_presence;Username=flowchat;Password=flowchat",
                ["Kafka:DuetConversationContactConsumer:BootstrapServers"] = "localhost:9092",
                ["Kafka:DuetConversationContactConsumer:GroupId"] = "presence-service",
                ["Kafka:DuetConversationContactConsumer:RetryGroupId"] = "presence-service-duet-conversation-contact-retry",
                ["Kafka:DuetConversationContactConsumer:Topic"] = "dev.flowchat.chat.duet-conversation-projection.v1",
                ["Kafka:DuetConversationContactConsumer:RetryTopic"] = "dev.flowchat.chat.duet-conversation-projection.v1.presence-service.retry",
                ["Kafka:DuetConversationContactConsumer:DeadLetterTopic"] = "dev.flowchat.chat.duet-conversation-projection.v1.presence-service.dlq",
                ["Kafka:DuetConversationContactConsumer:MaxRetryCount"] = "5",
                ["Kafka:DuetConversationContactConsumer:RetryBaseDelaySeconds"] = "5",
                ["Kafka:DuetConversationContactConsumer:RetryMaxDelaySeconds"] = "300",
                ["Kafka:DuetConversationContactConsumer:AutoOffsetReset"] = "Earliest",
                ["Kafka:DuetConversationContactConsumer:BatchSize"] = "100",
                ["Kafka:DuetConversationContactConsumer:BatchMaxWaitTimeMilliseconds"] = "1000"
            })
            .Build();
    }

    private static async Task<KafkaConsumerEndpointConfiguration> GetEndpointConfigurationAsync(
        Func<KafkaConsumerEndpointConfigurationBuilder<object>, KafkaConsumerEndpointConfigurationBuilder<object>> configureEndpoint)
    {
        await using var serviceProvider = new ServiceCollection().BuildServiceProvider();
        var builder = new KafkaConsumerConfigurationBuilder(serviceProvider)
            .WithBootstrapServers("localhost:9092")
            .WithGroupId("test-group")
            .WithAutoOffsetReset(AutoOffsetReset.Earliest)
            .Consume(endpoint => configureEndpoint(endpoint));

        var configuration = builder.Build();

        return configuration.Endpoints.Should().ContainSingle()
            .Which.Should().BeOfType<KafkaConsumerEndpointConfiguration>().Subject;
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
