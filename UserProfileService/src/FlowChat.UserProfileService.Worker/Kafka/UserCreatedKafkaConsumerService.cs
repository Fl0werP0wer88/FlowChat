using System.Text.Json;
using Confluent.Kafka;
using FlowChat.Messaging.Contracts.AuthService.Events;
using FlowChat.UserProfileService.Application.UserProfiles.Commands;
using MediatR;
using Microsoft.Extensions.Options;

namespace FlowChat.UserProfileService.Worker.Kafka;

public sealed class UserCreatedKafkaConsumerService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<UserCreatedKafkaConsumerService> _logger;
    private readonly UserCreatedConsumerOptions _options;

    public UserCreatedKafkaConsumerService(
        IServiceScopeFactory scopeFactory,
        ILogger<UserCreatedKafkaConsumerService> logger,
        IOptions<UserCreatedConsumerOptions> options)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _options = options.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var offsetReset = ParseAutoOffsetReset(_options.AutoOffsetReset);
        var config = new ConsumerConfig
        {
            BootstrapServers = _options.BootstrapServers,
            GroupId = _options.GroupId,
            AutoOffsetReset = offsetReset
        };

        using var consumer = new ConsumerBuilder<string, string>(config).Build();
        consumer.Subscribe(_options.Topic);

        _logger.LogInformation(
            "Kafka consumer started. Topic: {Topic}, GroupId: {GroupId}",
            _options.Topic,
            _options.GroupId);

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var result = consumer.Consume(stoppingToken);
                if (result.Message?.Value is null)
                {
                    continue;
                }

                await HandleMessageAsync(result, stoppingToken);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Kafka consumer stopping.");
        }
        catch (ConsumeException ex)
        {
            _logger.LogError(ex, "Kafka consume error: {Reason}", ex.Error.Reason);
        }
        finally
        {
            consumer.Close();
        }
    }

    private async Task HandleMessageAsync(
        ConsumeResult<string, string> consumeResult,
        CancellationToken cancellationToken)
    {
        var messageValue = consumeResult.Message.Value;

        UserCreatedEvent? message;

        try
        {
            message = JsonSerializer.Deserialize<UserCreatedEvent>(
                messageValue,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Invalid user-created payload: {Payload}", messageValue);
            return;
        }

        if (message is null)
        {
            _logger.LogWarning("Received empty user-created payload.");
            return;
        }

        await OnUserCreatedEventConsumedAsync(consumeResult, message, cancellationToken);
    }

    private async Task OnUserCreatedEventConsumedAsync(
        ConsumeResult<string, string> consumeResult,
        UserCreatedEvent message,
        CancellationToken cancellationToken)
    {
        var key = consumeResult.Message.Key;
        var messageValue = consumeResult.Message.Value;

        _logger.LogInformation(
            "Consumed user-created event at {TopicPartitionOffset} with key {Key}",
            consumeResult.TopicPartitionOffset,
            key);

        var userName = message.UserName?.Trim();
        if (string.IsNullOrWhiteSpace(userName))
        {
            _logger.LogWarning("Payload does not contain UserName: {Payload}", messageValue);
            return;
        }

        var displayName = ResolveDisplayName(message, userName);
        var userId = ResolveUserId(message.UserId, key);
        if (!userId.HasValue)
        {
            _logger.LogWarning(
                "Payload does not contain valid UserId. Key: {Key}, Payload: {Payload}",
                key,
                messageValue);
            return;
        }

        using var scope = _scopeFactory.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        try
        {
            await mediator.Send(
                new CreateInitialUserProfileCommand(
                    userName,
                    displayName,
                    null,
                    null,
                    userId.Value),
                cancellationToken);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogInformation(
                ex,
                "Skipping user profile creation for {UserName}. {Reason}",
                userName,
                ex.Message);
        }
    }

    private static AutoOffsetReset ParseAutoOffsetReset(string value)
    {
        return Enum.TryParse<AutoOffsetReset>(value, true, out var parsed)
            ? parsed
            : AutoOffsetReset.Earliest;
    }

    private static Guid? ResolveUserId(Guid payloadUserId, string? key)
    {
        if (payloadUserId != Guid.Empty)
        {
            return payloadUserId;
        }

        return Guid.TryParse(key, out var keyAsGuid)
            ? keyAsGuid
            : null;
    }

    private static string ResolveDisplayName(UserCreatedEvent message, string userName)
    {
        if (!string.IsNullOrWhiteSpace(message.DisplayName))
        {
            return message.DisplayName.Trim();
        }

        var firstName = message.FirstName?.Trim();
        var lastName = message.LastName?.Trim();
        var fullName = $"{firstName} {lastName}".Trim();

        return string.IsNullOrWhiteSpace(fullName)
            ? userName
            : fullName;
    }
}

