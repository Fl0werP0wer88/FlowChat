# FlowChat Testing Conventions

This file contains all testing conventions for the FlowChat project. Follow these EXACTLY when generating tests.

## Tech Stack

- **Framework**: xUnit (`[Fact]`, `[Theory]`)
- **Assertions**: FluentAssertions (`Should().BeTrue()`, `Should().ContainSingle()`, etc.)
- **Mocking**: Moq (`Mock<T>`, `.Setup()`, `.Verify()`)
- **Test data**: AutoFixture (`IFixture`, `_fixture.Create<T>()`)
- **Result type**: `FlowChatResult<T>` from `FlowChat.Shared`

## Naming Convention

```
MethodName_Scenario_ExpectedResult
```

Examples:
- `Handle_CreatesUser_AndDispatchesAccountRegisteredDomainEvent`
- `Handle_WhenRepositoryReportsDuplicateUserName_ReturnsConflictFailure`
- `Create_WithInvalidAddress_Throws`
- `AddEmail_WhenDuplicate_ThrowsInvalidOperationException`

## File & Folder Structure

- Mirror the `src/` folder structure inside the test project
- Source: `{Service}/src/FlowChat.{Service}.{Layer}/{path}/{Class}.cs`
- Unit test: `{Service}/tests/FlowChat.{Service}.UnitTests/{path}/{Class}Tests.cs`
- Integration test: `{Service}/tests/FlowChat.{Service}.IntegrationTests/{path}/{Class}Tests.cs`

## Layer-Specific Rules

### Domain Layer (Unit Tests — NO mocks)

Domain entities are pure C# objects. Test them directly:

```csharp
using FlowChat.Shared.Domain;
using FlowChat.Shared.Domain.ValueObjects;
using FlowChat.{Service}.Domain.Entities;
using FlowChat.{Service}.Domain.Events;

namespace FlowChat.{Service}.UnitTests;

public sealed class {ClassName}Tests
{
    [Fact]
    public void Create_WithValidArgs_CreatesEntity()
    {
        var entity = Entity.Create(/* args */);

        entity.Should().NotBeNull();
        entity.Property.Should().Be(expectedValue);
    }
}
```

Key rules:
- NO mocks — entities are pure objects
- Test `Create(...)` factory — valid and invalid inputs
- Test state-changing methods — verify property changes
- Assert domain events via `entity.DomainEvents.OfType<T>()`
- Assert `AggregateStateChangedDomainEvent` where applicable
- Do NOT assert domain events after `Restore(...)` — it intentionally raises none
- Value objects: test `Create(...)`, equality, validation failures

### Application Layer — Command/Query Handlers (Unit Tests — mock boundaries)

```csharp
using AutoFixture;
using FlowChat.{Service}.Application.Contracts.Persistence;
using FlowChat.{Service}.Application.Features.{Feature}.Commands.{Command};
using FlowChat.Shared.Application;
using FlowChat.Shared.Domain;
using FluentAssertions;
using Moq;

namespace FlowChat.{Service}.UnitTests;

public sealed class {Handler}Tests
{
    private readonly IFixture _fixture = new Fixture();
    private readonly Mock<IRepository> _repositoryMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<IDomainEventDispatcher> _domainEventDispatcherMock = new();
    private readonly {Handler} _handler;

    public {Handler}Tests()
    {
        // Setup UoW to just execute the operation
        _unitOfWorkMock
            .Setup(x => x.ExecuteInTransactionAsync(
                It.IsAny<Func<CancellationToken, Task<FlowChatResult<{Response}>>>>(),
                It.IsAny<CancellationToken>()))
            .Returns<Func<CancellationToken, Task<FlowChatResult<{Response}>>>, CancellationToken>(
                (operation, ct) => operation(ct));

        // Setup domain event dispatcher
        _domainEventDispatcherMock
            .Setup(x => x.DispatchAsync(It.IsAny<IEnumerable<IDomainEvent>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _handler = new {Handler}(
            _repositoryMock.Object,
            _unitOfWorkMock.Object,
            _domainEventDispatcherMock.Object);
    }

    [Fact]
    public async Task Handle_HappyPath_ReturnsSuccess()
    {
        // Arrange
        _repositoryMock.Setup(x => x.Method(It.IsAny<T>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedValue);

        // Act
        var result = await _handler.Handle(CreateCommand(), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
    }

    private {Command} CreateCommand() => new(/* use _fixture for values */);
}
```

Key rules:
- Mock ONLY at boundaries: repositories, UoW, domain event dispatcher, infrastructure services
- ALWAYS assert `FlowChatResult<T>` — check `IsSuccess`/`IsFailure` AND the value/error
- ALWAYS assert domain events dispatched (capture via Callback on dispatcher mock)
- Use `_fixture.Create<T>()` for test data, not hardcoded values (unless the value matters for the test)

### Application Layer — Domain Event Handlers (Unit Tests)

```csharp
public sealed class {EventHandler}Tests
{
    [Fact]
    public async Task Handle_MapsToIntegrationEvent_AndPublishes()
    {
        // Test that domain event is correctly mapped to integration event
    }
}
```

### Infrastructure Layer (Unit Tests + Integration Tests)

Unit tests for isolated services (JWT generator, settings manager):
- Mock external dependencies (IConfiguration, IOptions)
- Test logic in isolation

Integration tests for DI registration and cross-layer wiring:
- Build real `ServiceCollection`
- Use `BuildServiceProvider()` to verify resolution
- No mocks — test real wiring

### Persistence Layer (Integration Tests)

```csharp
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

public sealed class {Repository}Tests
{
    [Fact]
    public async Task Method_Scenario_ExpectedResult()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var dbContext = CreateDbContext(connection);

        // Test with real database
    }

    private static AppDbContext CreateDbContext(SqliteConnection connection)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connection)
            .Options;

        var context = new AppDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }
}
```

Key rules:
- Use SQLite in-memory or EF InMemoryDatabase — NOT real Postgres
- Test repositories, Unit of Work, interceptors
- No mocks — the point is verifying real DB interaction

### API Layer — Controllers (Unit Tests)

```csharp
public sealed class {Controller}Tests
{
    private readonly IFixture _fixture = new Fixture();
    private readonly Mock<IMediator> _mediatorMock = new();

    [Fact]
    public async Task Endpoint_WhenValidRequest_ReturnsExpectedResult()
    {
        _mediatorMock
            .Setup(x => x.Send(It.IsAny<{Command}>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(FlowChatResult<{Response}>.Success(new {Response}()));

        var controller = CreateController();

        var result = await controller.Method(request, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }
}
```

### Workers / Consumers (Unit Tests + Integration Tests)

- Unit test: mock HTTP clients, verify message handling logic
- Integration test: verify DI registration, consumer configuration

---

## Integration Test Tooling (Advanced)

Beyond simple SQLite/InMemory tests, use these tools for higher-fidelity integration tests:

### Microsoft.AspNetCore.Mvc.Testing — API Integration Tests

Use `WebApplicationFactory<T>` to spin up the real HTTP pipeline (middleware, routing, DI, serialization) without a network socket.

**This project already uses this pattern** — see `UserProfileService/tests/.../API/UserProfileApiFactory.cs` as the reference implementation.

Key conventions from the existing factory:

```csharp
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection.Extensions;

// Use a CONTROLLER type as anchor — not Program — to avoid ambiguity
// when a service has multiple hosts (API + Consumers worker both define Program).
public sealed class {Service}ApiFactory : WebApplicationFactory<{AnchorController}>, IAsyncLifetime
{
    public const string InternalApiKey = "test-internal-api-key";
    private readonly SqliteConnection _connection = new("DataSource=:memory:");

    public RecordingIntegrationEventPublisher EventPublisher { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Test");

        // Override config values — Kafka, connection strings, feature URLs
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["FlowChat:InternalApi:ApiKey"] = InternalApiKey,
                ["ConnectionStrings:{Service}Db"] = "Host=localhost;Database=test",
                // ... Kafka topics, feature URLs, etc.
            });
        });

        // ConfigureTestServices runs AFTER all app services — overrides are guaranteed
        builder.ConfigureTestServices(services =>
        {
            // Remove Silverback hosted services (needs real Kafka broker)
            services.RemoveAll<IHostedService>();

            // Replace AppDbContext with SQLite — remove ALL related registrations
            // including IDbContextOptionsConfiguration<T> to avoid
            // "Multiple relational database provider configurations found"
            services.RemoveAll<DbContextOptions<AppDbContext>>();
            services.RemoveAll<IDbContextFactory<AppDbContext>>();
            services.RemoveAll<AppDbContext>();
            services.RemoveAll<IDbContextOptionsConfiguration<AppDbContext>>();

            services.AddDbContext<AppDbContext>((sp, options) =>
                options.UseSqlite(_connection)
                    .AddInterceptors(sp.GetRequiredService<EntityBaseSaveChangesInterceptor>()));

            // Replace IIntegrationEventPublisher with recording stub
            services.RemoveAll<IIntegrationEventPublisher>();
            services.AddSingleton<IIntegrationEventPublisher>(EventPublisher);

            // Ephemeral data protection — no DB-backed key store needed
            services.RemoveAll<IXmlRepository>();
            services.AddDataProtection().UseEphemeralDataProtectionProvider();
        });
    }

    // Helper for seeding/asserting DB state
    public async Task WithDbContextAsync(Func<AppDbContext, Task> action)
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await action(db);
    }

    public async Task InitializeAsync()
    {
        await _connection.OpenAsync();

        // Create schema BEFORE host starts — data protection reads tables on startup
        var opts = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_connection).Options;
        await using (var db = new AppDbContext(opts))
            await db.Database.EnsureCreatedAsync();

        _ = Services; // triggers host startup
    }

    public new async Task DisposeAsync()
    {
        base.Dispose();
        await _connection.DisposeAsync();
    }
}
```

Key gotchas (learned from the existing implementation):
- Use a **controller type** as `WebApplicationFactory<T>` anchor, not `Program` — avoids ambiguity when multiple hosts exist in the same service
- Remove `IDbContextOptionsConfiguration<AppDbContext>` — without this, Npgsql callback fires alongside SQLite → "Multiple relational database provider configurations found"
- Remove `IHostedService` registrations — Silverback registers hosted services that need a real Kafka broker
- Create DB schema **before** host starts — data protection background thread reads tables on startup
- Use `ConfigureTestServices` (not `ConfigureServices`) — runs after all app registrations, guaranteeing overrides apply

When to use:
- Testing full HTTP request/response cycle (status codes, content negotiation, headers)
- Verifying middleware (auth, validation, error handling)
- Verifying route mapping and model binding
- Testing integration events are published via `RecordingIntegrationEventPublisher`
- Smoke-testing that the app boots and critical endpoints respond

When NOT to use:
- For testing business logic — use unit tests against handlers
- When you only need DI resolution checks — use plain `ServiceCollection` tests

### Testcontainers — Real Infrastructure in Tests

Use `Testcontainers.Kafka` (or `Testcontainers.PostgreSql`) when you need a real broker/database, not a mock:

```csharp
using DotNet.Testcontainers.Builders;
using Testcontainers.Kafka;

public sealed class KafkaIntegrationTests : IAsyncLifetime
{
    private readonly KafkaContainer _kafka = new KafkaBuilder()
        .WithImage("confluentinc/cp-kafka:7.6.0")
        .Build();

    public Task InitializeAsync() => _kafka.StartAsync();
    public Task DisposeAsync() => _kafka.DisposeAsync().AsTask();

    [Fact]
    public async Task Producer_PublishesMessage_ConsumerReceives()
    {
        var bootstrapServers = _kafka.GetBootstrapAddress();

        // Use real Kafka producer/consumer with bootstrapServers
        // Verify end-to-end message flow
    }
}
```

When to use:
- Testing Kafka producer/consumer end-to-end (message serialization, topic routing, consumer groups)
- Testing against real PostgreSQL when SQLite diverges (JSON columns, array types, full-text search)
- Verifying Outbox pattern end-to-end (DB → Outbox → Kafka)

When NOT to use:
- For unit tests — too slow, use mocks
- For simple repository CRUD — SQLite in-memory is sufficient
- In CI without Docker — guard with `[Trait("Category", "Docker")]`

NuGet packages:
- `Testcontainers` (base)
- `Testcontainers.Kafka`
- `Testcontainers.PostgreSql`

### WireMock.Net — HTTP Dependency Stubs

Use `WireMock.Server` to stub external HTTP APIs (internal service-to-service calls, third-party APIs):

```csharp
using WireMock.Server;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;

public sealed class AuthInternalApiClientTests : IDisposable
{
    private readonly WireMockServer _server;
    private readonly AuthInternalApiClient _client;

    public AuthInternalApiClientTests()
    {
        _server = WireMockServer.Start();

        var httpClient = new HttpClient { BaseAddress = new Uri(_server.Url!) };
        _client = new AuthInternalApiClient(httpClient);
    }

    [Fact]
    public async Task ConfirmEmail_WhenServiceReturns200_ReturnsSuccess()
    {
        _server
            .Given(Request.Create()
                .WithPath("/api/auth/confirm-email")
                .UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithBody("{}"));

        var result = await _client.ConfirmEmailAsync("user@example.com", "token123");

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task ConfirmEmail_WhenServiceReturns500_ReturnsFailure()
    {
        _server
            .Given(Request.Create()
                .WithPath("/api/auth/confirm-email")
                .UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(500));

        var result = await _client.ConfirmEmailAsync("user@example.com", "token123");

        result.IsFailure.Should().BeTrue();
    }

    public void Dispose() => _server.Dispose();
}
```

When to use:
- Testing HTTP clients that call other FlowChat services (e.g. `AuthInternalApiClient`)
- Simulating error responses (timeouts, 500s, malformed JSON)
- Verifying retry/circuit-breaker policies on HttpClient
- Testing webhook/callback handlers

When NOT to use:
- For in-process service calls (use `WebApplicationFactory` instead)
- When a simple `Mock<HttpMessageHandler>` suffices for one-off tests

NuGet package: `WireMock.Net`

---

## What NOT to Test

- Simple DTOs / records with no logic
- `GlobalUsings.cs`
- Constants files (unless they contain computed values)
- `*ServiceRegistration.cs` — test via integration tests that verify DI resolution, not the registration code itself
- Marker interfaces
- EF Migrations

## FlowChatResult Assertion Patterns

```csharp
// Success
result.IsSuccess.Should().BeTrue();
result.Value.Should().NotBeNull();
result.Value.Id.Should().Be(expectedId);

// Failure
result.IsFailure.Should().BeTrue();
result.Error.ErrorType.Should().Be(ErrorType.Conflict);
result.Error.ErrorMessage.Should().Contain("already exists");

// NEVER just check IsSuccess — always check the value or error too
```

## Domain Event Assertion Patterns

```csharp
// On aggregates (after state change)
entity.DomainEvents.OfType<MyDomainEvent>().Should().ContainSingle()
    .Which.PropertyName.Should().Be(expectedValue);

// On dispatched events (via mock callback)
List<IDomainEvent> dispatchedEvents = [];
_dispatcherMock
    .Setup(x => x.DispatchAsync(It.IsAny<IEnumerable<IDomainEvent>>(), It.IsAny<CancellationToken>()))
    .Callback<IEnumerable<IDomainEvent>, CancellationToken>((events, _) => dispatchedEvents.AddRange(events))
    .Returns(Task.CompletedTask);

// Assert after act
dispatchedEvents.Should().ContainSingle()
    .Which.Should().BeOfType<MyDomainEvent>();
```

## Common GlobalUsings for Test Projects

```csharp
// UnitTests/GlobalUsings.cs
global using FlowChat.Core.Results;
global using FluentAssertions;
global using Xunit;
```
