# FlowChat — Claude Code Instructions

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
Located in `DevInfrastructure/`: PostgreSQL, Kafka, MailHog, Observability stack.

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
- **Results**: use `FlowChatResult<T>` (from `FlowChat.Shared`) instead of throwing exceptions in handlers
- **Entities**: use `static Create(...)` factory methods, never public constructors
- **Domain events**: raise via `AddDomainEvent(...)` inside the entity
- **Restore from DB**: use `static Restore(...)` — does NOT raise domain events

## Testing

- **Unit tests**: xUnit + FluentAssertions + Moq + AutoFixture
  - Location: `{Service}/tests/{Service}.UnitTests/`
  - Mirror the `src/` folder structure inside the test project — if a file moves or a new folder is added in `src/`, update the corresponding location in `tests/` accordingly
  - Mock only at layer boundaries (Application → Persistence, Application → Infrastructure, Workers → HTTP clients); Domain layer tests need no mocks at all — entities are pure C# objects
  - Test naming: `MethodName_Scenario_ExpectedResult`
- **Asserting results**: always verify `FlowChatResult<T>` explicitly — check `IsSuccess`/`IsFailure` and the returned value or error, not just what was passed to a mock
- **Domain events**: command handler tests should assert domain events raised on the aggregate (via `DomainEvents.OfType<T>()`) in addition to the return value — events drive the Outbox and Kafka integration, so they are part of the observable behaviour
- **`Restore(...)` factory**: do not assert domain events after calling `Restore(...)` — it intentionally does not raise any
- **When to write/update tests:**
  - Add or update tests when behaviour changes: public interface, business logic, error handling, or a bug is being fixed
  - When refactoring without behaviour change: keep existing tests as-is; adjust only if they no longer compile or structurally mismatch
  - For new features: cover the happy path, relevant edge cases, and known failure modes
  - Do not write low-value tests just to have coverage (e.g. testing that a constructor assigns a property)
- **AATs** (Application Acceptance Tests): `{Service}/tests/{Service}.AATs/`

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
dotnet test Common/tests/FlowChat.Shared.Persistance.UnitTests
dotnet test RealtimeService/tests/FlowChat.RealtimeService.UnitTests
```

## What to Avoid

- Do not use `AutoMapper` — mapping is done manually or via dedicated profile classes
- Do not add `try/catch` inside command handlers — use `FlowChatResult` instead
- Do not put business logic in controllers or infrastructure layer
- Do not raise domain events in `Restore(...)` factory methods
- Do not use `DateTime.Now` — use `DateTime.UtcNow`
