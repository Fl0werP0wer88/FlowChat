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
  - Mirror the `src/` folder structure inside the test project
  - Mock only external dependencies (repositories, event dispatchers, HTTP clients)
  - Test naming: `MethodName_Scenario_ExpectedResult`
- **AATs** (Application Acceptance Tests): `{Service}/tests/{Service}.AATs/`

Run tests:
```bash
dotnet test AuthService/FlowChat.AuthService.slnx
```

## What to Avoid

- Do not use `AutoMapper` — mapping is done manually or via dedicated profile classes
- Do not add `try/catch` inside command handlers — use `FlowChatResult` instead
- Do not put business logic in controllers or infrastructure layer
- Do not raise domain events in `Restore(...)` factory methods
- Do not use `DateTime.Now` — use `DateTime.UtcNow`
