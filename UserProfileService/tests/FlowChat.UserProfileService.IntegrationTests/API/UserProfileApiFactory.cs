using FlowChat.Shared.Application;
using FlowChat.UserProfileService.Api.Features.UserProfiles.Internal.CreateInitialUserProfile;
using FlowChat.UserProfileService.Persistence;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.DataProtection.Repositories;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FlowChat.UserProfileService.IntegrationTests.API;

// CreateInitialUserProfileController is used as anchor type to unambiguously identify
// the API assembly — both the API and the Consumers worker define a top-level Program class.
public sealed class UserProfileApiFactory : WebApplicationFactory<CreateInitialUserProfileController>, IAsyncLifetime
{
    public const string InternalApiKey = "test-internal-api-key";
    private readonly SqliteConnection _connection = new("DataSource=:memory:");

    public RecordingIntegrationEventPublisher EventPublisher { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Test");

        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["FlowChat:InternalApi:ApiKey"] = InternalApiKey,
                ["ConnectionStrings:UserProfileDb"] = "Host=localhost;Database=test",
                ["Kafka:UserProfileCreatedProducer:BootstrapServers"] = "localhost:9092",
                ["Kafka:UserProfileCreatedProducer:Topic"] = "test.user-profile-created",
                ["Kafka:UserEmailConfirmedProducer:BootstrapServers"] = "localhost:9092",
                ["Kafka:UserEmailConfirmedProducer:Topic"] = "test.user-email-confirmed",
                ["Kafka:UserEmailVerificationRequestedProducer:BootstrapServers"] = "localhost:9092",
                ["Kafka:UserEmailVerificationRequestedProducer:Topic"] = "test.email-verification",
                ["Kafka:UserProfileStateChangedProducer:BootstrapServers"] = "localhost:9092",
                ["Kafka:UserProfileStateChangedProducer:Topic"] = "test.user-profile-state",
                ["ConfirmationLinks:EmailVerificationBaseUrl"] = "https://test.example.com/verify",
                ["ApiUrl"] = "https://localhost",
                ["BlazorUrl"] = "https://localhost"
            });
        });

        builder.ConfigureLogging(logging =>
        {
            logging.ClearProviders();
            logging.AddConsole();
            logging.SetMinimumLevel(LogLevel.Warning);
        });

        // ConfigureTestServices runs AFTER all application services are registered,
        // so these removals are guaranteed to apply on top of the fully-configured service collection.
        builder.ConfigureTestServices(services =>
        {
            // Silverback reads BootstrapServers from config at DI registration time (before factory
            // config overrides apply), so it registers with an empty string. Remove its hosted service
            // so the app boots without needing a running Kafka broker.
            services.RemoveAll<IHostedService>();

            // Replace all AppDbContext registrations with SQLite.
            // AddDbContext stores per-context config callbacks as IDbContextOptionsConfiguration<TContext> —
            // we must remove those too, otherwise the Npgsql callback is still invoked when building options,
            // causing "Multiple relational database provider configurations found".
            services.RemoveAll<DbContextOptions<AppDbContext>>();
            services.RemoveAll<IDbContextFactory<AppDbContext>>();
            services.RemoveAll<AppDbContext>();
            services.RemoveAll<IDbContextOptionsConfiguration<AppDbContext>>();

            services.AddDbContext<AppDbContext>((sp, options) =>
                options.UseSqlite(_connection)
                    .AddInterceptors(sp.GetRequiredService<FlowChat.Shared.Persistance.Auditing.EntityBaseSaveChangesInterceptor>()));

            services.AddDbContextFactory<AppDbContext>((sp, options) =>
                options.UseSqlite(_connection)
                    .AddInterceptors(sp.GetRequiredService<FlowChat.Shared.Persistance.Auditing.EntityBaseSaveChangesInterceptor>()),
                ServiceLifetime.Scoped);

            // Replace IIntegrationEventPublisher with recording stub
            services.RemoveAll<IIntegrationEventPublisher>();
            services.AddSingleton<IIntegrationEventPublisher>(EventPublisher);

            // Replace DataProtection with ephemeral provider so we don't need the DB-backed key store.
            // Also remove IXmlRepository so the non-ephemeral KeyRingProvider doesn't still try
            // to read from DataProtectionKeys via PersistKeysToDbContext.
            services.RemoveAll<IXmlRepository>();
            services.AddDataProtection().UseEphemeralDataProtectionProvider();
        });
    }

    public async Task<T> WithDbContextAsync<T>(Func<AppDbContext, Task<T>> action)
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return await action(db);
    }

    public async Task WithDbContextAsync(Func<AppDbContext, Task> action)
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await action(db);
    }

    public async Task InitializeAsync()
    {
        await _connection.OpenAsync();

        // Create the SQLite schema BEFORE starting the host.
        // The data protection key ring initializes in a background thread during host startup
        // and tries to read the DataProtectionKeys table; the table must exist by then.
        // We build AppDbContext directly (bypassing DI) so the host hasn't started yet.
        var schemaOptions = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .Options;
        await using (var db = new AppDbContext(schemaOptions))
        {
            await db.Database.EnsureCreatedAsync();
        }

        // Now start the host (accessing Services triggers WebApplicationFactory startup)
        _ = Services;
    }

    public new async Task DisposeAsync()
    {
        base.Dispose();
        await _connection.DisposeAsync();
    }
}
