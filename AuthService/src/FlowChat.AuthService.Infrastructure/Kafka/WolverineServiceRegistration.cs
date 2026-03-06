using FlowChat.AuthService.Persistence;
using FlowChat.Messaging.Contracts.AuthService.Events;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Wolverine;
using Wolverine.Kafka;
using Wolverine.Postgresql;

namespace FlowChat.AuthService.Infrastructure.Kafka;

public static class WolverineServiceRegistration
{
    public static TBuilder AddWolverineMessaging<TBuilder>(this TBuilder builder)
        where TBuilder : IHostApplicationBuilder
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.UseWolverine(options => ConfigureWolverine(options, builder.Configuration));

        return builder;
    }

    private static void ConfigureWolverine(WolverineOptions options, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(configuration);

        var userCreatedOptions = ResolveUserCreatedProducerOptions(configuration);
        var emailVerificationOptions = ResolveEmailVerificationProducerOptions(configuration);
        var connectionString = configuration.GetConnectionString("AuthDb")
            ?? throw new InvalidOperationException("Missing connection string: AuthDb.");
        var bootstrapServers = ResolveBootstrapServers(userCreatedOptions, emailVerificationOptions);

        options.UseKafka(bootstrapServers);
        options.PersistMessagesWithPostgresql(connectionString)
            .Enroll<AppDbContext>();
        options.Policies.UseDurableOutboxOnAllSendingEndpoints();

        options.PublishMessage<UserCreatedIntegrationEvent>()
            .ToKafkaTopic(userCreatedOptions.Topic);
        options.PublishMessage<UserConfirmedIntegrationEvent>()
            .ToKafkaTopic(userCreatedOptions.Topic);
        options.PublishMessage<EmailVerificationRequestIntegrationEvent>()
            .ToKafkaTopic(emailVerificationOptions.Topic);
    }

    private static string ResolveBootstrapServers(
        KafkaProducerSettings userCreatedOptions,
        KafkaProducerSettings emailVerificationOptions)
    {
        var bootstrapServers = new[]
        {
            userCreatedOptions.BootstrapServers,
            emailVerificationOptions.BootstrapServers
        }
        .Where(value => !string.IsNullOrWhiteSpace(value))
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .ToArray();

        return bootstrapServers.Length switch
        {
            1 => bootstrapServers[0],
            0 => "localhost:9092",
            _ => throw new InvalidOperationException(
                "AuthService Wolverine configuration requires a single Kafka bootstrap server for all published integration events.")
        };
    }

    private static KafkaProducerSettings ResolveUserCreatedProducerOptions(IConfiguration configuration)
    {
        var producerSection = configuration.GetSection(UserCreatedProducerOptions.SectionName);
        var fallbackSection = configuration.GetSection(UserCreatedProducerOptions.FallbackSectionName);

        return new KafkaProducerSettings
        {
            BootstrapServers = producerSection["BootstrapServers"]
                ?? fallbackSection["BootstrapServers"]
                ?? "localhost:9092",
            Topic = producerSection["Topic"]
                ?? fallbackSection["Topic"]
                ?? "dev.flowchat.identity.user.v1"
        };
    }

    private static KafkaProducerSettings ResolveEmailVerificationProducerOptions(IConfiguration configuration)
    {
        var producerSection = configuration.GetSection(UserEmailVerificationRequestedProducerOptions.SectionName);
        var fallbackSection = configuration.GetSection(UserEmailVerificationRequestedProducerOptions.FallbackSectionName);

        return new KafkaProducerSettings
        {
            BootstrapServers = producerSection["BootstrapServers"]
                ?? fallbackSection["BootstrapServers"]
                ?? "localhost:9092",
            Topic = producerSection["Topic"]
                ?? fallbackSection["Topic"]
                ?? "dev.flowchat.identity.user.v1"
        };
    }

    private sealed class KafkaProducerSettings
    {
        public string BootstrapServers { get; init; } = "localhost:9092";
        public string Topic { get; init; } = "dev.flowchat.identity.user.v1";
    }
}
