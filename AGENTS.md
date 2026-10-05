# FlowChat — Claude & Codex Instructions

## Solution Files

Every new `.csproj` must be added to **both**:
1. The local service solution ( `{Service}/FlowChat.{Service}.slnx`)
2. The global solution `FlowChat.slnx` in the root — in the appropriate service folder

## Adding A New Service Or Other Bootable Workspace Entry

When asked to add a new service or any other bootable project/solution to the workspace, update the workspace and repo coordination files as well as the code projects.

- `FlowChat.code-workspace`
  - Add a new entry to `folders` so the service appears as a separate workspace folder
  - Add or update `launch.compounds` for the new service so the root workspace can start its bootable projects
  - Update shared compounds such as `All APIs`, `All Workers`, and `All Services` when the new executable should participate in those startup sets
  - Keep build tasks aligned only when an extra explicit build step is needed; regular `.NET` service projects are usually picked up through `FlowChat.slnx`
- `FlowChat.slnx`
  - Add the top-level service folder and all relevant `src/`, `src/Workers/`, and `tests/` projects in the correct solution folders
- `{Service}/FlowChat.{Service}.slnx`
  - Add every local project for that service, including bootable projects and tests
  - Keep any shared `Common` project entries aligned with the existing service-solution pattern
- `{Service}/.vscode/launch.json`
  - Add launch configurations for every bootable project in that service, for example `API`, `Consumers`, `OutboxPublisher`, or other executable hosts
  - Add or update the local compound that starts the service from that folder
- `{Service}/.vscode/tasks.json`
  - Add matching build tasks for each launch configuration referenced in the local launch file
- `{Service}/.vscode/settings.json`
  - Keep the local VS Code settings file in place so the service mirrors the existing per-service workspace setup
- `Scripts/Infrastructure/PostgreSQL/migration-services.json`
  - If the new service has `Persistence` plus a startup project, add it to the shared service inventory so both bulk migration scripts include it
- `Scripts/*`
  - Review infrastructure scripts only when the new service introduces new shared resources such as database migrations, Kafka topics, Redis usage, or other dev-stack dependencies; the shared migration service inventory is `Scripts/Infrastructure/PostgreSQL/migration-services.json`
- `Scripts/Infrastructure/PostgreSQL/*`
  - If the new service needs its own database, update the PostgreSQL scripts immediately so the database is created and maintained according to the existing rules, privileges, and naming conventions
  - Keep database naming aligned with the current pattern used by `bootstrap-postgres.ps1`, for example `flowchat_<service>_db`
  - At minimum review the Bash and PowerShell versions of `bootstrap-postgres`, `migrate-all`, and `reset-db` so bootstrap, bulk migration, and reset flows all include the new database consistently
- `AGENTS.md` and `CLAUDE.md`
  - Add the new service under `### Services` with its repo location (for example `{Service}/`) and a short responsibility/description in both files
  - Update any explicit service-specific examples or command lists in both files when the new service should be part of them

After creating a new service folder, treat files like `AuthService/.vscode/*` and `AuthService/FlowChat.AuthService.slnx` as the template for the new service's local workspace setup.

## Collaboration Rules

- If the user's message ends with `?`, treat it as a question — answer it, do not make any code changes unless explicitly asked afterwards.
- If you edit `AGENTS.md` or `CLAUDE.md`, apply the same changes to the other file so both instruction files stay synchronized.
- At the end of every completed code or repository change, include a proposed pull request title in the final response.
- When referencing a specific place in code, always include a clickable file-and-line link in addition to the file name and code snippet, so the user can jump directly to that location.
- If the model needs to create any temporary working files (for example decompiled library output, scratch files, generated investigation artifacts, or similar), create them under the tool-specific temp folder in the repository root: `.codex/temp` for Codex and `.claude/temp` for Claude.
- When a change removes the last remaining usage of a method, class, interface, or other element without relocating that element elsewhere, check whether it has become orphaned (no other references left anywhere in the codebase). If so, remove the orphaned element as part of the same change rather than leaving dead code behind.

## Project Overview

FlowChat is a microservices-based chat application built with .NET 10. Services communicate via Kafka (integration events) and expose REST APIs through a gateway.

### Services
- **PresenceService** — user presence statuses and contact-based fan-out projection
- **AuthService** — registration, login, email/phone confirmation, JWT tokens
- **ChatService** — chat rooms and messages; also owns contacts (a Duet conversation is the contact relationship — block/mute/hide are per-participant state on it)
- **NotificationService** — email/SMS notifications
- **GatewayService** — API gateway
- **RealtimeService** — SignalR real-time connections
- **UserProfileService** — user profiles
- **HarnessService** — dev-only general-purpose test harness for AAT-testing cross-cutting infrastructure patterns (projection pipeline, Kafka retry/DLQ isolation, etc.); located in `HarnessService/`

### Dev Infrastructure (Docker)
Located in `Scripts/Infrastructure/`: PostgreSQL, Kafka, MailHog, Observability stack.

## Architecture

Each service follows **Clean Architecture**:
- `Domain` — entities, domain events, value objects (no external dependencies)
- `Application` — CQRS commands/queries (MediatR), contracts/interfaces
- `Infrastructure` — Kafka producers/consumers, JWT, external clients
- `Persistence` — EF Core, repositories, Unit of Work, Outbox pattern
- `API` — controllers, minimal API endpoints
- `Workers` — background workers (e.g. outbox publisher)

GatewayService intentionally uses only `API` and `Infrastructure`: API owns HTTP endpoints and orchestration facades, while Infrastructure owns downstream service clients and transport concerns. Do not add empty Application, Domain, or Persistence projects to GatewayService.

Domain events are dispatched via `IDomainEventDispatcher` and mapped to integration events published to Kafka.

### Read repositories
- Read repositories must not query or project from Domain aggregates/entities/value objects; the read side is persistence-only
- Use dedicated persistence read entities in `{Service}.Persistence/Entities`, mapped to existing tables/views with simple column types (`Guid`, `string`, enums, `DateTimeOffset`, etc.)
- Read entities inherit from `FlowChat.Shared.Persistance.ReadEntityBase`, never from Domain `EntityBase<T>`, `IEntity<T>`, `IAuditableEntity`, or auditable persistence `EntityBase`
- Keep read entities free of domain behavior, typed domain IDs, domain value objects, and domain event logic
- Keep write repositories on Domain aggregates; this rule applies to read repositories and read-side EF projections only
- Map read entities to DTO/read models inside read repositories or projection helpers, preserving public Application/API contracts

### API and Application boundaries
- Controllers do not call repositories or persistence services directly
- A controller's role is limited to HTTP concerns: reading the request, authorization/authentication, invoking the appropriate command/query through MediatR, and mapping HTTP DTOs and responses
- GatewayService is exempt from the MediatR requirement because it is a proxy and aggregation layer: its controllers invoke dedicated orchestration facades instead; shared service-to-service translation and validation belong in those facades, never in controllers
- Request validation belongs in the Application layer via FluentValidation / MediatR pipeline; in GatewayService, orchestration facades validate their inputs and return `FlowChatResult`, rather than placing validation in controllers
- Never accept the current user's ID as an explicit route parameter, query parameter, or request body field on authenticated endpoints — always extract it from the JWT claim via `TryGetCurrentUserId(out var userId)` inherited from `ApiControllerBase`; passing the caller's identity in the request lets clients impersonate other users
- Internal endpoints (API-key-authenticated, service-to-service) are exempt and may accept explicit user IDs in their payloads

### API request/response naming and mapping
- Root request/response types follow `{Action}{Resource}Request` / `{Action}{Resource}Response`; only the root type implements `IServiceInput`/`IServiceOutput`
- Nested composite objects inside a response also use the `Response` suffix (e.g. `ParticipantResponse`, `ContactResponse`), never `Dto` — a `Dto` suffix on an API-facing type signals an Application/Persistence type has leaked across the boundary
- Define nested response types in the same file as their root response, one set per feature folder; duplicate the shape per feature instead of sharing one type across features (mirrors how `ParticipantResponse` is redefined per conversation feature in ChatService)
- Map Application/Persistence DTOs to these API response types via a dedicated AutoMapper `Profile` class colocated in the same feature folder (e.g. `{Feature}MappingProfile.cs`), injected into the controller as `IMapper` — do not build the response with manual `.Select(...)` projections or private static `MapToResponse` helper methods in the controller
- Root responses that fan-in data from multiple independent sources (e.g. a gateway aggregating several service clients) are the exception — compose the root manually, but still map any nested per-source collections through the registered `IMapper`
- Unit tests for a controller that takes `IMapper` must construct a real mapper from the feature's profile (`new MapperConfiguration(cfg => cfg.AddProfile<XMappingProfile>(), NullLoggerFactory.Instance).CreateMapper()`), not a mock — this exercises the actual mapping instead of asserting against a stub

### Application contract placement
- Keep interfaces in `Application/Contracts/*` only when their implementations live outside the `Application` project, for example in `Infrastructure`, `Persistence`, `API`, or `Workers`
- If an interface is implemented inside the same `Application` layer, keep it next to the implementing class in a local `Interfaces/` folder within that feature slice instead of `Application/Contracts/*`
- When moving or creating such interfaces, keep namespaces aligned with the feature folder structure and update tests to mirror the `src/` layout

### Application eventing structure
- Place  aggregate-specific domain event handlers inside the vertical slice of the aggregate that produces the event
- In `Application/Features/{Aggregate}/`, event-related files live under `Eventing/`
- `Eventing/` contains two subfolders: `DomainEvents/` and `ApplicationEvents/`
- Keep `ApplicationEvents/` present even when it is temporarily empty
- Under `DomainEvents/`, create one folder per event named after the event/handler stem without the `DomainEventHandler` suffix, for example `UserProfileCreated/` or `EmailConfirmed/`
- Store files that belong only to that event inside its folder, such as the `*DomainEventHandler` and any dedicated AutoMapper `Profile` used to map that event to an integration event
- Split event-to-integration-event AutoMapper mappings into separate profiles per event instead of using one aggregate-wide profile
- Keep only truly shared eventing infrastructure in `Common/Eventing`, such as base handler classes or reusable abstractions
- Keep namespaces aligned with the folder structure after every move

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

### Domain invariants
- Enforce inside the entity/aggregate — throw `ArgumentException` for invalid input, `InvalidOperationException` for violated business rules
- Do not validate domain rules in command handlers or controllers

## Coding Conventions

- **Language**: C# 13, .NET 10
- **Nullability**: nullable reference types enabled everywhere
- **Time handling**: always prefer `DateTimeOffset` over `DateTime`; when representing UTC time, use `DateTimeOffset` with offset `+00:00`
- **Results**: use `FlowChatResult<T>` (from `FlowChat.Shared`) instead of throwing exceptions in handlers
- **CQRS commands/queries**: use only primitive/simple scalar types at the application boundary (`string`, numeric types, `bool`, `Guid`, `DateTimeOffset`, enums, and collections of those when needed); do not pass domain entities or value objects in commands/queries
- **Domain modeling**: in the `Domain` layer, prefer existing value objects wherever reasonable instead of raw primitives; first look in `Common`, then in the local service
- **Value object suggestions**: if you see a field that is a good fit for a value object but none exists yet in `Common` or the local service, explicitly suggest creating one
- **Entities**: use `static Create(...)` factory methods, never public constructors
- **Domain events**: raise via `AddDomainEvent(...)` inside the entity
- **Restore from DB**: use `static Restore(...)` — does NOT raise domain events
- **App settings loading**: always load `appsettings` sections via `AppSettingsProvider` from `Common/src/FlowChat.Shared.Infrastructure/Configuration/AppSettingsProvider.cs`; do not bind sections directly via raw `IConfiguration.GetSection(...).Get<T>()` in application code when the provider can be used
- **Settings section placement**: classes representing `appsettings` sections must implement `ISettingSection` from `Common/src/FlowChat.Core/Contracts/ISettingSection.cs` and must live in the project-level `Configuration/Settings` folder, for example `{Project}/Configuration/Settings/*SettingsSection.cs`, not directly in `Configuration` and not in feature folders such as `Kafka`
- **Kafka topic naming for projections**: any Kafka topic carrying a `ProjectionIntegrationEvent<TValue>` published via `PublishProjectionIntegrationEventProcessor` (`Common/src/FlowChat.Shared.Application/CommandHandlers/AggregateRootCommandHandlerBaseV2/BeforeSaveProcessors/PublishProjectionIntegrationEventProcessor.cs`) must use a topic name ending in `-projection` (for example `dev.flowchat.chat.duet-conversation-projection.v1`), never a topic shared with bespoke domain integration events — this keeps "genuine domain event" topics visually and structurally separate from "technical projection-sync" topics. If a domain event and a projection event happen to share a producer settings section/topic today, split the projection off onto its own dedicated `-projection` topic rather than reusing the shared one.
- **Kafka topic naming for regular integration events**: a Kafka topic carrying a bespoke domain/integration event (not a `ProjectionIntegrationEvent<TValue>`) follows `dev.flowchat.<domain>.<resource>.v1`, for example `dev.flowchat.chat.message.v1` or `dev.flowchat.identity.user.v1`. Tiered retry topics append `.{service}.retry.{delay}` and the dead-letter topic appends `.{service}.dlq`; for example `dev.flowchat.chat.message.v1.realtime-service.retry.5s` and `dev.flowchat.chat.message.v1.realtime-service.dlq`. Never append `-projection` to a topic used by a regular integration event.
- **Service registration placement**: classes registering project services such as `ApplicationServiceRegistration`, `APIServiceRegistration`, `InfrastructureServiceRegistration`, `PersistenceServiceRegistration`, `ConsumersServiceRegistration`, `OutboxPublisherServiceRegistration`, or `SilverbackServiceRegistration` must live directly in the project-level `Configuration` folder, for example `{Project}/Configuration/*ServiceRegistration.cs`, not in feature folders such as `Kafka` and not in `Configuration/Settings`
- **Aggregate before-save processor placement**: never register a reusable `IAggregateBeforeSaveProcessor<TCommand, TAggregate>` implementation from `Common` directly. Add an aggregate-specific class in the Application feature's `Processors/` folder, named for the aggregate and responsibility (for example `ContactProjectionProcessor<TCommand>`), inherit from the reusable Common implementation there, and register that concrete aggregate-specific class. This keeps every processor affecting an aggregate discoverable from the aggregate's Application feature folder without inspecting DI registration.
- Do mappings via dedicated profile classes for AutoMapper on Application, Infrastructure & Api layers. On Domain layer all mapping must be done manually in dedicated method.

### Marker interfaces
- Marker interfaces from `Common/src/FlowChat.Core/Contracts` and `Common/src/FlowChat.Core/Messaging` classify transport and projection models by role; add them whenever creating a new contract of the matching kind
- `IServiceEndpoint` is the common marker for service endpoint contracts
- `IServiceInput` marks request models declared in `API` projects
- `IServiceOutput` marks response models declared in `API` projects
- `IConsumerOutput` marks request contracts emitted by worker consumers to other internal endpoints
- `IConsumerInput` marks payloads consumed by workers; integration events implement this through the `IntegrationEvent` base class
- `IDbResponse` marks read models and DTOs that are direct EF Core projection targets in queries and repositories
- If a model changes role, update its marker interface to match the new responsibility instead of keeping the previous classification

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
  - If a rule here and an example in `patterns.md` seem to diverge, keep the architectural rule from these instructions and adapt the example to the current codebase rather than copying it blindly
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
dotnet test PresenceService/FlowChat.PresenceService.slnx
dotnet test UserProfileService/FlowChat.UserProfileService.slnx

# Common & standalone (no solution file)
dotnet test Common/tests/FlowChat.Shared.API.UnitTests
dotnet test Common/tests/FlowChat.Shared.Persistance.IntegrationTests
dotnet test Common/tests/FlowChat.Shared.Persistance.UnitTests
dotnet test RealtimeService/tests/FlowChat.RealtimeService.IntegrationTests
dotnet test RealtimeService/tests/FlowChat.RealtimeService.UnitTests
```

## Code Comments

Add comments only where they provide information that cannot be derived by reading the code — the **why**, not the **what**.

### Add a comment when:
- A business rule or domain invariant is enforced and the reason is not obvious from the code alone (e.g. why a method is idempotent, why a specific constant value was chosen)
- A deliberate design decision was made that a future reader might question or "fix" incorrectly (e.g. why `ownTransaction: false`, why a generic error message is used for all failure paths)
- An edge case is handled that would not be apparent without domain or infrastructure context (e.g. Kafka redelivery guard, EF duplicate-tracking workaround)
- Security-sensitive reasoning must be preserved (e.g. timing attack resistance, information leakage prevention)

### Do NOT add a comment when:
- The code reads like plain English and the intent is self-evident
- The comment merely restates what the code does (e.g. `// increment counter` above `counter++`)
- The information is already captured in the class/method name, XML docs, or a test name
- The code is a simple CRUD operation, DTO, mapping, or DI registration

### Format rules:
- One sentence preferred; two sentences maximum
- No period at the end of a single-sentence inline comment
- Place the comment on the line immediately above the relevant code, not inline at the end of the line (except for single-value annotations like `Roles = [] // reason`)
- Use `//` only — no block comments (`/* */`) in application code

### When modifying existing code:
- Read comments already present in the file before adding new ones
- Update a comment if the behaviour it describes has changed — stale comments are worse than no comments
- Do not duplicate a comment that already exists nearby

## What to Avoid


- Do not add `try/catch` inside command handlers — use `FlowChatResult` instead
- Do not put business logic in controllers or infrastructure layer
- Do not raise domain events in `Restore(...)` factory methods
- Do not introduce `DateTime` for timestamps or UTC values — use `DateTimeOffset` in UTC instead
