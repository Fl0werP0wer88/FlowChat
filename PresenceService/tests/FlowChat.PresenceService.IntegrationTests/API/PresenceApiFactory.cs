using FlowChat.PresenceService.API.Features.Presence.Public.ChangePresenceStatus;
using FlowChat.PresenceService.Application.Contracts.Infrastructure;
using FlowChat.PresenceService.Persistence;
using FlowChat.Shared.Persistance.Auditing;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace FlowChat.PresenceService.IntegrationTests.API;

public sealed class PresenceApiFactory : WebApplicationFactory<ChangePresenceStatusController>, IAsyncLifetime
{
    public const string InternalApiKey = "test-presence-internal-key";
    private readonly SqliteConnection _connection = new("DataSource=:memory:");

    public RecordingIntegrationEventPublisher EventPublisher { get; } = new();
    public InMemoryPresenceStatusStore PresenceStatusStore { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Test");

        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["FlowChat:InternalApi:ApiKey"] = InternalApiKey,
                ["JwtSettings:Key"] = "FLOWCHAT_TEST_JWT_KEY_CHANGE_ME_123456789",
                ["JwtSettings:Issuer"] = "https://localhost:7236/",
                ["JwtSettings:Audience"] = "FlowChat.Client",
                ["ConnectionStrings:PresenceDb"] = "Host=localhost;Database=test",
                ["ConnectionStrings:Redis"] = "localhost:6379,user=default,password=flowchat_redis_pw",
                ["Kafka:PresenceStatusChangedProducer:BootstrapServers"] = "localhost:9092",
                ["Kafka:PresenceStatusChangedProducer:Topic"] = "test.presence.presence"
            });
        });

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IHostedService>();

            services.RemoveAll<DbContextOptions<AppDbContext>>();
            services.RemoveAll<IDbContextFactory<AppDbContext>>();
            services.RemoveAll<AppDbContext>();
            services.RemoveAll<IDbContextOptionsConfiguration<AppDbContext>>();

            services.AddDbContext<AppDbContext>((serviceProvider, options) => options
                .UseSqlite(_connection)
                .AddInterceptors(serviceProvider.GetRequiredService<EntityBaseSaveChangesInterceptor>()));
            services.AddDbContextFactory<AppDbContext>(
                (serviceProvider, options) => options
                    .UseSqlite(_connection)
                    .AddInterceptors(serviceProvider.GetRequiredService<EntityBaseSaveChangesInterceptor>()),
                ServiceLifetime.Scoped);

            services.RemoveAll<IOutboxIntegrationEventPublisher>();
            services.AddSingleton<IOutboxIntegrationEventPublisher>(EventPublisher);

            services.RemoveAll<IPresenceStatusStore>();
            services.AddSingleton(PresenceStatusStore);
            services.AddSingleton<IPresenceStatusStore>(PresenceStatusStore);

            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = TestAuthenticationHandler.SchemeName;
                options.DefaultChallengeScheme = TestAuthenticationHandler.SchemeName;
            }).AddScheme<AuthenticationSchemeOptions, TestAuthenticationHandler>(
                TestAuthenticationHandler.SchemeName,
                _ => { });
        });
    }

    public async Task WithDbContextAsync(Func<AppDbContext, Task> action)
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await action(db);
    }

    public async Task<T> WithDbContextAsync<T>(Func<AppDbContext, Task<T>> action)
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return await action(db);
    }

    public async Task InitializeAsync()
    {
        await _connection.OpenAsync();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .AddInterceptors(new EntityBaseSaveChangesInterceptor())
            .Options;

        await using (var db = new AppDbContext(options))
        {
            await db.Database.EnsureCreatedAsync();
        }

        _ = Services;
    }

    public new async Task DisposeAsync()
    {
        base.Dispose();
        await _connection.DisposeAsync();
    }
}
