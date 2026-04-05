# FlowChat — Claude Code Instructions

## Solution Files

Every new `.csproj` must be added to **both**:
1. The local service solution ( `{Service}/FlowChat.{Service}.slnx`)
2. The global solution `FlowChat.slnx` in the root — in the appropriate service folder

## Collaboration Rules

- If the user's message ends with `?`, treat it as a question — answer it, do not make any code changes unless explicitly asked afterwards.
- If the model needs to create any temporary working files (for example decompiled library output, scratch files, generated investigation artifacts, or similar), create them under `.codex/temp` in the repository root.

## Project Overview

FlowChat is a microservices-based chat application built with .NET 10. Services communicate via Kafka (integration events) and expose REST APIs through a gateway.

### Services
- **AuthService** — registration, login, email/phone confirmation, JWT tokens
- **ChatService** — chat rooms and messages
- **NotificationService** — email/SMS notifications
- **GatewayService** — API gateway
- **RealtimeService** — SignalR real-time connections
- **SocialGraphService** — friends/followers graph
- **UserProfileService** — user profiles

### Dev Infrastructure (Docker)
Located in `Scripts/`: PostgreSQL, Kafka, MailHog, Observability stack.

## Architecture

Each service follows **Clean Architecture**:
- `Domain` — entities, domain events, value objects (no external dependencies)
- `Application` — CQRS commands/queries (MediatR), contracts/interfaces
- `Infrastructure` — Kafka producers/consumers, JWT, external clients
- `Persistence` — EF Core, repositories, Unit of Work, Outbox pattern
- `API` — controllers, minimal API endpoints
- `Workers` — background workers (e.g. outbox publisher)

Domain events are dispatched via `IDomainEventDispatcher` and mapped to integration events published to Kafka.

## Domain-Driven Design

The project uses tactical DDD. All domain logic lives in the `Domain` layer. These rules are non-negotiable:

### Aggregate roots
- Inherit from `AggregateRootBase<T>`
- All state changes go through the aggregate root's public methods — never mutate child entities from outside
- Child entity mutation methods are `internal` (e.g. `Email.Confirm()`, `Email.SetMain()`) — only the aggregate root may call them
- One repository per aggregate root — there is no `IEmailRepository`, only `IUserProfileWriteRepository`

### Entities
- Inherit from `EntityBase<T>`
- Always use typed `Id<T>` — never raw `Guid` in domain code; `Id<T>` prevents accidental cross-aggregate ID mixing
- Reference other aggregates by `Id<T>` only, never by object (e.g. `Id<UserProfile> UserProfileId`, not `UserProfile UserProfile`)

### Value objects
- Immutable — all properties are read-only
- Equality by value — implement `IEquatable<T>`, `==`, `!=`
- Always use `static Create(...)` factory; private constructor
- Validate and throw `ArgumentException` on invalid input inside `Create(...)`

### Domain events
- Raised inside the aggregate via `AddDomainEvent(...)` as a result of a state change — never from outside
- `AggregateStateChangedDomainEvent<TAggregate, TSnapshot>` is a special event that carries a snapshot of the aggregate state; call `MarkAggregateStateChanged(...)` after every state-changing operation — it is automatically deduplicated (only the latest snapshot is kept per operation)

### Domain invariants
- Enforce inside the entity/aggregate — throw `ArgumentException` for invalid input, `InvalidOperationException` for violated business rules
- Do not validate domain rules in command handlers or controllers

## Coding Conventions

- **Language**: C# 13, .NET 10
- **Formatting**: enforced by `dprint` — run `dprint fmt` before committing
- **Nullability**: nullable reference types enabled everywhere
- **Time handling**: always prefer `DateTimeOffset` over `DateTime`; when representing UTC time, use `DateTimeOffset` with offset `+00:00`
- **Results**: use `FlowChatResult<T>` (from `FlowChat.Shared`) instead of throwing exceptions in handlers
- **CQRS commands/queries**: use only primitive/simple scalar types at the application boundary (`string`, numeric types, `bool`, `Guid`, `DateTimeOffset`, enums, and collections of those when needed); do not pass domain entities or value objects in commands/queries
- **Domain modeling**: in the `Domain` layer, prefer existing value objects wherever reasonable instead of raw primitives; first look in `Common`, then in the local service
- **Value object suggestions**: if you see a field that is a good fit for a value object but none exists yet in `Common` or the local service, explicitly suggest creating one
- **Entities**: use `static Create(...)` factory methods, never public constructors
- **Domain events**: raise via `AddDomainEvent(...)` inside the entity
- **Restore from DB**: use `static Restore(...)` — does NOT raise domain events

## Testing

- **Tech stack**: xUnit + FluentAssertions + Moq + AutoFixture
- **Result type**: `FlowChatResult<T>` from `FlowChat.Shared`
- **Test naming**: `MethodName_Scenario_ExpectedResult`
- **File structure**:
  - Mirror the `src/` folder structure inside the test project — if a file moves or a new folder is added in `src/`, update the corresponding location in `tests/` accordingly
  - Unit test path: `{Service}/tests/FlowChat.{Service}.UnitTests/{path}/{Class}Tests.cs`
  - Integration test path: `{Service}/tests/FlowChat.{Service}.IntegrationTests/{path}/{Class}Tests.cs`
  - AAT path: `{Service}/tests/FlowChat.{Service}.AATs/`

- **Unit tests**:
  - Domain layer tests use no mocks at all — entities/value objects are pure C# objects
  - Mock only at layer boundaries: Application → Persistence, Application → Infrastructure, Workers → HTTP clients
  - Use `_fixture.Create<T>()` for test data unless a literal value is important to the assertion
  - Always verify `FlowChatResult<T>` explicitly — check `IsSuccess`/`IsFailure` and the returned value or error, not just what was passed to a mock
  - Command/query handler tests should capture and assert dispatched domain events, not just the return value
  - Domain tests should assert domain events via `entity.DomainEvents.OfType<T>()`
  - Assert `AggregateStateChangedDomainEvent<TAggregate, TSnapshot>` where applicable
  - Do not assert domain events after `Restore(...)` — it intentionally raises none

- **Layer-specific expectations**:
  - Domain: test `Create(...)`, validation failures, state-changing methods, value-object equality and invariants
  - Application handlers: mock repositories/UoW/domain event dispatcher/infrastructure services only; assert result shape and dispatched events
  - Application domain event handlers: verify mapping to integration events and publishing behaviour
  - Infrastructure unit tests: test isolated services such as JWT generation or settings management with mocked dependencies
  - API controller unit tests: mock `IMediator`, verify status code/payload/problem details, and validate request-to-command mapping where relevant
  - Workers/consumers unit tests: mock HTTP clients or external services and verify message handling logic

- **Integration tests**: `{Service}/tests/FlowChat.{Service}.IntegrationTests/`
  - Use when a test builds a real `ServiceCollection` + `BuildServiceProvider()`, uses a real `DbContext` (even in-memory), tests DI registration, or validates startup configuration across multiple layers
  - Do NOT mock at layer boundaries — the point is to verify the layers work together
  - If a file has a mix of unit and integration tests, split it into separate files
  - Prefer `Sqlite` in-memory or `UseInMemoryDatabase`; do not use a real Postgres instance unless the scenario genuinely requires provider-specific behaviour
  - Persistence integration tests should cover repositories, Unit of Work, EF interceptors, and real DB interaction
  - DI/startup integration tests should verify service registration, host bootstrapping, consumer/producer wiring, and critical service resolution
  - Do NOT write integration tests for business logic already covered by domain/handler unit tests
  - Do NOT duplicate coverage: if a unit test already proves the behaviour, an integration test of the same scenario adds noise rather than safety

- **Advanced integration tooling**:
  - Use `WebApplicationFactory<T>` for full HTTP pipeline tests; in services with multiple hosts prefer a controller type as the anchor instead of `Program`
  - For `WebApplicationFactory` overrides, use `ConfigureTestServices`, remove conflicting `DbContext` registrations including `IDbContextOptionsConfiguration<AppDbContext>`, remove hosted services that need Kafka, and create the test DB schema before host startup
  - Use `Testcontainers.Kafka` / `Testcontainers.PostgreSql` only when you need real infrastructure behaviour such as end-to-end Kafka flow, Outbox-to-Kafka verification, or provider-specific Postgres features that SQLite cannot model
  - Use `WireMock.Net` for HTTP dependency stubs when testing service-to-service HTTP clients or retry/error handling

- **Canonical test patterns**:
  - The canonical source for test style and example code is `.claude/skills/generate-tests/patterns.md`
  - When generating tests, follow the patterns from that file for structure, mocking boundaries, result assertions, domain event assertions, `WebApplicationFactory`, `Testcontainers`, and `WireMock.Net`
  - If a rule here and an example in `patterns.md` seem to diverge, keep the architectural rule from `AGENTS.md` and adapt the example to the current codebase rather than copying it blindly
  - Prefer matching an existing project pattern over inventing a new test style

```csharp
// Handler test: UoW pass-through + dispatcher capture
_unitOfWorkMock
    .Setup(x => x.ExecuteInTransactionAsync(
        It.IsAny<Func<CancellationToken, Task<FlowChatResult<TResponse>>>>(),
        It.IsAny<CancellationToken>()))
    .Returns<Func<CancellationToken, Task<FlowChatResult<TResponse>>>, CancellationToken>(
        (operation, ct) => operation(ct));

List<IDomainEvent> dispatchedEvents = [];
_domainEventDispatcherMock
    .Setup(x => x.DispatchAsync(It.IsAny<IEnumerable<IDomainEvent>>(), It.IsAny<CancellationToken>()))
    .Callback<IEnumerable<IDomainEvent>, CancellationToken>((events, _) => dispatchedEvents.AddRange(events))
    .Returns(Task.CompletedTask);
```

```csharp
// FlowChatResult assertions
result.IsSuccess.Should().BeTrue();
result.Value.Should().NotBeNull();

result.IsFailure.Should().BeTrue();
result.Error.ErrorType.Should().Be(ErrorType.Conflict);
result.Error.ErrorMessage.Should().Contain("already exists");
```

```csharp
// Domain event assertions
entity.DomainEvents.OfType<MyDomainEvent>().Should().ContainSingle()
    .Which.PropertyName.Should().Be(expectedValue);

dispatchedEvents.Should().ContainSingle()
    .Which.Should().BeOfType<MyDomainEvent>();
```

```csharp
// WireMock.Net for HTTP client tests
_server
    .Given(Request.Create().WithPath("/api/example").UsingPost())
    .RespondWith(Response.Create().WithStatusCode(200).WithBody("{}"));
```

- **What to write and what to avoid**:
  - Add or update tests when behaviour changes: public interface, business logic, error handling, or a bug fix
  - When refactoring without behaviour change, keep existing tests as-is and adjust only when they no longer compile or structurally mismatch
  - For new features, cover the happy path, relevant edge cases, and known failure modes
  - Do not write low-value tests just for coverage
  - Do not test simple DTOs/records with no logic, `GlobalUsings.cs`, constants files without behaviour, marker interfaces, or EF migrations
  - Do not unit test `*ServiceRegistration.cs` directly — cover DI registration through integration tests

- **After every code change:**
  1. Run unit tests for the affected service (`dotnet test {Service}/FlowChat.{Service}.slnx`)
  2. Fix any failures before continuing — do not leave a test suite red while working on the next thing
  3. Run integration tests for the affected service when the change touches: DI registration, EF Core / persistence, Kafka producers or consumers, command handler wiring, API controller mapping, or startup/host configuration
  4. Before marking a task as done, run the full test suite for the affected service and report: what passed, what failed, and any known risks or untested edge cases
  5. Never ignore a failing test unless the user explicitly instructs it — if a pre-existing test breaks, investigate before continuing

Run tests:
```bash
# All services at once
dotnet test FlowChat.slnx

# Per service
dotnet test AuthService/FlowChat.AuthService.slnx
dotnet test ChatService/FlowChat.ChatService.slnx
dotnet test NotificationService/FlowChat.NotificationService.slnx
dotnet test SocialGraphService/FlowChat.SocialGraphService.slnx
dotnet test UserProfileService/FlowChat.UserProfileService.slnx

# Common & standalone (no solution file)
dotnet test Common/tests/FlowChat.Shared.API.UnitTests
dotnet test Common/tests/FlowChat.Shared.Persistance.IntegrationTests
dotnet test Common/tests/FlowChat.Shared.Persistance.UnitTests
dotnet test RealtimeService/tests/FlowChat.RealtimeService.IntegrationTests
dotnet test RealtimeService/tests/FlowChat.RealtimeService.UnitTests
```

## What to Avoid

- Do not use `AutoMapper` — mapping is done manually or via dedicated profile classes
- Do not add `try/catch` inside command handlers — use `FlowChatResult` instead
- Do not put business logic in controllers or infrastructure layer
- Do not raise domain events in `Restore(...)` factory methods
- Do not introduce `DateTime` for timestamps or UTC values — use `DateTimeOffset` in UTC instead
