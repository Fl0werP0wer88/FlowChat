# FlowChat

[Polish version](ReadMe.PL.txt) | [English text version](ReadMe.EN.txt)

## Table of contents

- [1. System architecture](#1-system-architecture)
  - [1.1. Microservice boundaries and data ownership](#11-microservice-boundaries-and-data-ownership)
  - [1.2. Clean Architecture layers](#12-clean-architecture-layers)
  - [1.3. CQRS, DDD, and result handling](#13-cqrs-ddd-and-result-handling)
  - [1.4. Asynchronous communication, Outbox, and resilience](#14-asynchronous-communication-outbox-and-resilience)
    - [1.4.1. Cross-service projections](#141-cross-service-projections)
    - [1.4.2. Retry and DLQ](#142-retry-and-dlq)
  - [1.5. Real-time communication](#15-real-time-communication)
- [2. Component responsibilities](#2-component-responsibilities)
  - [2.1. ReactClient](#21-reactclient)
  - [2.2. GatewayService](#22-gatewayservice)
  - [2.3. AuthService](#23-authservice)
  - [2.4. UserProfileService](#24-userprofileservice)
  - [2.5. ChatService](#25-chatservice)
  - [2.6. PresenceService](#26-presenceservice)
  - [2.7. RealtimeService](#27-realtimeservice)
  - [2.8. NotificationService](#28-notificationservice)
  - [2.9. HarnessService](#29-harnessservice)
  - [2.10. Common](#210-common)
- [3. Technology stack](#3-technology-stack)
  - [Backend and API](#backend-and-api)
  - [Application architecture](#application-architecture)
  - [Data](#data)
  - [Messaging and integration](#messaging-and-integration)
  - [Security](#security)
  - [Frontend](#frontend)
  - [Notifications](#notifications)
  - [Observability](#observability)
  - [Tests](#tests)
  - [Local infrastructure](#local-infrastructure)
- [4. Key business flows](#4-key-business-flows)
  - [4.1. User registration](#41-user-registration)
  - [4.2. Sending a message](#42-sending-a-message)
  - [4.3. Changing presence status](#43-changing-presence-status)
- [5. Repository structure](#5-repository-structure)
- [6. Configuration and local startup](#6-configuration-and-local-startup)

FlowChat is a web-based chat application built using a microservice architecture.
The system supports user registration and authentication, profiles, private and group
conversations, messages, contacts, presence statuses, notifications, and real-time
communication. The backend is built on .NET 10, while the user interface is a React
application running in the browser.

The repository is a monorepo containing all backend services, shared libraries,
the web client, tests, and scripts for running the local infrastructure.

## 1. System architecture

FlowChat combines several complementary architectural styles:

- microservices divide the system into independent business areas,
- Clean Architecture separates domain logic from transport and infrastructure,
- Domain-Driven Design (DDD) models business rules using aggregates, entities,
  value objects, and domain events,
- CQRS separates state-changing operations (commands) from reads (queries),
- vertical slices organize code by feature rather than only by class type,
- synchronous communication uses REST APIs,
- asynchronous communication uses events published to Apache Kafka,
- SignalR delivers events to connected clients in real time.

Simplified communication flow:

```text
+-------------------+
| ReactClient       |
| browser           |
+---------+---------+
          |
          | HTTPS / REST / SignalR
          v
+---------+---------+
| GatewayService    |
| YARP + aggregation|
+---------+---------+
          |
          | synchronous HTTP calls
          v
+---------+-----------------------------------------------------------+
| Auth | Chat | UserProfile | Presence | Realtime | Notification     |
+---------+-----------------------------------------------------------+
          |                         |
          | EF Core                 | Outbox Publisher / Consumers
          v                         v
+---------+---------+      +--------+---------+
| PostgreSQL        |      | Apache Kafka     |
| database/service  |      | events           |
+-------------------+      +--------+---------+
                                    |
                                    v
                           +--------+---------+
                           | RealtimeService  |
                           | SignalR + Redis  |
                           +--------+---------+
                                    |
                                    v
                            connected clients
```

### 1.1. Microservice boundaries and data ownership

Each service owns its domain model and PostgreSQL database. Services do not directly
read tables owned by other services. Data required by another business area is obtained
through an API or replicated as a local projection based on Kafka events. As a result,
developing and deploying one service does not require sharing its database schema with
the other services.

The write model uses domain aggregates. The read model uses simple persistence entities
and DTOs, so queries do not have to reconstruct an entire aggregate.

### 1.2. Clean Architecture layers

Business services are divided into Domain, Application, Persistence, Infrastructure,
and API projects. Kafka processes run as separate Worker hosts: the Consumers host
receives events, while OutboxPublisher publishes events stored in the service database.

GatewayService is an intentional exception. As a proxy and aggregation layer, it contains
only API and Infrastructure projects and does not own a domain model or database.

### 1.3. CQRS, DDD, and result handling

Controllers pass requests to MediatR and do not access repositories directly. Input
validation belongs to the Application layer. Business rules and invariants are enforced
by the domain model, and aggregate state changes can produce domain events.

Handlers return `FlowChatResult<T>`, which explicitly represents either success or
failure. This allows application errors to be mapped to predictable HTTP responses
without using exceptions to control normal business flow.

### 1.4. Asynchronous communication, Outbox, and resilience

A business change and its corresponding Outbox entry are stored in a single database
transaction. A separate OutboxPublisher reads pending entries and publishes them to
Kafka. This prevents an event from being lost when the data has been saved but the broker
is temporarily unavailable.

Consumers process integration events independently of the API process. Message handling
uses multiple retry levels with increasing delays. An event that cannot be processed is
moved to a dedicated dead-letter topic (DLQ), isolating the failed message from normal
traffic.

The system distinguishes between:

- integration events describing business events,
- projection events synchronizing technical read models between services.

Projection topics use the `-projection` suffix and remain separate from business-event
topics. This makes Kafka flows easier to observe and troubleshoot.

#### 1.4.1. Cross-service projections

- the source service stores a projection event in its Outbox and publishes it to Kafka,
- the consumer validates the event and maps it to a `ProjectionSingle` command,
- projection data and the Kafka offset are stored in one EF Core transaction,
- the UPSERT accounts for the aggregate version, so an older or duplicate event cannot
  overwrite newer data,
- a projection is a local read-only copy and does not add its own business rules.

#### 1.4.2. Retry and DLQ

- a transient failure, such as temporary database or service unavailability, moves the
  message from the main topic to the first retry topic,
- FlowChat uses four retry levels after 5, 20, 60, and 300 seconds; every level has a
  separate Kafka topic,
- the message carries `RetryAtUtc`, the attempt number, the last error type, and the
  original topic, partition, and offset,
- the retry consumer pauses the relevant partition until `RetryAtUtc`, then resumes it
  and attempts processing again,
- another transient failure moves the message to the next retry level,
- a permanent or unknown failure, another failure requiring isolation, or exhaustion of
  all retry attempts moves the message to a dedicated dead-letter topic (DLQ),
- writing the message to a retry topic or DLQ and committing the source offset happen
  atomically through the Silverback EF Outbox, protecting the message from being lost
  during the move,
- the DLQ isolates a failed message from normal traffic and preserves the information
  required for diagnosis and a later controlled replay.

Retries are not performed directly on the main topic. Separate topics prevent a message
that temporarily cannot be processed from blocking the primary stream.

### 1.5. Real-time communication

RealtimeService exposes an authorized SignalR hub at `/hubs/chat`. It registers active
user connections in Redis, manages SignalR groups, and forwards information about new
messages, conversation changes, and presence statuses to clients.

Events from ChatService and PresenceService are received by the
RealtimeService.Consumers process. This means business services do not need to know
about individual browser connections or communicate with clients directly.

## 2. Component responsibilities

### 2.1. ReactClient

The web client built with React and TypeScript. It provides the user interface,
communicates with the backend through GatewayService, and receives SignalR events.

### 2.2. GatewayService

The public entry point to the backend. It uses YARP to route requests to the appropriate
services, verifies access, and aggregates responses requiring data from multiple sources.

### 2.3. AuthService

Manages accounts, login, logout, passwords, access tokens, and refresh tokens. It
publishes events describing user identity creation and changes.

### 2.4. UserProfileService

Manages user profiles, names, email addresses, and phone numbers. It supports profile
search, email verification, and publishing profile data required by other services.

### 2.5. ChatService

Manages private and group conversations, messages, participants, and read state. It also
owns contacts, which are modeled as Duet conversations, and participant settings such as
blocking, muting, and hiding a conversation.

### 2.6. PresenceService

Manages current presence statuses and user preferences. It stores fast-changing state
in Redis and sends presence changes to interested contacts.

### 2.7. RealtimeService

Maintains SignalR connections and delivers message, conversation, and presence events
to clients. The connection registry is stored in Redis, allowing the service to run as
multiple instances.

### 2.8. NotificationService

Receives notification requests from Kafka and records their delivery status. The current
implementation sends email messages used to verify user addresses through SMTP.

### 2.9. HarnessService

A development-only helper service for testing shared infrastructure, including
projections, Kafka processing, multi-level retries, and DLQ behavior. It is not a product
feature.

### 2.10. Common

A set of libraries sharing the basic DDD, CQRS, persistence, API, integration, and
consumer mechanisms. It contains technical building blocks common to the services but
not their business logic.

## 3. Technology stack

### Backend and API

- C# 13
- .NET 10 / ASP.NET Core 10
- Web API controllers and separate Worker hosts
- SignalR
- YARP Reverse Proxy
- OpenAPI / Swagger

### Application architecture

- Clean Architecture
- Domain-Driven Design
- CQRS and MediatR
- FluentValidation
- AutoMapper
- `FlowChatResult<T>` / CSharpFunctionalExtensions

### Data

- PostgreSQL 16
- Entity Framework Core 10
- Npgsql
- Redis 7 and StackExchange.Redis
- a separate database for each business service

### Messaging and integration

- Apache Kafka
- Silverback
- Confluent.Kafka
- Transactional Outbox
- retry topics and dead-letter topics
- REST/HTTPS between the gateway and services

### Security

- OpenIddict
- JWT Bearer
- access tokens and refresh tokens
- Argon2id password hashing
- authorization policies in GatewayService
- API keys for internal service-to-service communication
- Infisical for local secret management

### Frontend

- React 19
- TypeScript 5
- Vite 7
- React Router
- TanStack Query
- Zustand
- Axios
- Microsoft SignalR Client
- React Virtuoso
- Vitest

### Notifications

- MailKit / SMTP
- MailHog in the development environment

### Observability

- OpenTelemetry and OTLP export
- Grafana Alloy
- Prometheus — metrics
- Loki — logs
- Tempo — distributed traces
- Grafana — visualization

### Tests

- xUnit
- FluentAssertions
- Moq and AutoFixture
- WebApplicationFactory
- SQLite/InMemory for integration tests
- Testcontainers for PostgreSQL, Kafka, and Redis
- unit, integration, and AAT tests

### Local infrastructure

- Docker / Docker Compose
- PowerShell scripts for starting and resetting the environment
- Kafka UI and RedisInsight for inspecting infrastructure

## 4. Key business flows

### 4.1. User registration

1. ReactClient sends a registration request through GatewayService.
2. AuthService creates the account, hashes the password, and stores the data together
   with an Outbox entry.
3. OutboxPublisher publishes the user-created event to Kafka.
4. UserProfileService.Consumers creates a user profile in its database.
5. UserProfileService publishes the profile projection for interested services.
6. The email verification request reaches NotificationService through Kafka.
7. NotificationService sends an SMTP message containing the confirmation link.

### 4.2. Sending a message

1. An authenticated client sends a message through GatewayService to ChatService.
2. ChatService verifies the conversation rules and stores the message and Outbox event.
3. OutboxPublisher publishes the message event to Kafka.
4. RealtimeService.Consumers receives the event and routes it to the RealtimeService API.
5. RealtimeService resolves active recipient connections and sends the event via SignalR.
6. Clients update the conversation view without polling the API for every change.

### 4.3. Changing presence status

1. PresenceService updates the user's current status and preferences.
2. It uses the contact projection to identify users observing the change.
3. The presence event is published and received by RealtimeService.
4. SignalR delivers the change only to the relevant connected clients.

## 5. Repository structure

- `FlowChat.slnx` — the global .NET solution containing the official backend and test
  projects.
- `FlowChat.code-workspace` — the main VS Code workspace with launch configurations for
  APIs, workers, the React client, and the `All APIs`, `All Workers`, and `All Services`
  compounds.
- `{Service}/FlowChat.{Service}.slnx` — a service-specific solution convenient for
  working on a single business area.
- `{Service}/src` — production code for the service layers and Worker projects.
- `{Service}/tests` — unit, integration, and, where required, AAT tests.
- `Common/src` and `Common/tests` — shared libraries and their tests.
- `ReactClient` — the React + TypeScript application.
- `Scripts` — Docker Compose files and PowerShell scripts for PostgreSQL, Kafka, Redis,
  MailHog, Infisical, and the observability stack.

## 6. Configuration and local startup

Requirements:

- .NET 10 SDK,
- Node.js 20 or later,
- Docker Engine with the Compose plugin or Docker Desktop,
- Bash or PowerShell,
- `jq`, `curl`, and `openssl` for the Bash bootstrap (`openssl` is used by Infisical),
- optionally, VS Code with `FlowChat.code-workspace` open.

Docker Compose files and configuration are located in `Scripts/Infrastructure/`,
under `PostgreSQL`, `Kafka`, `Redis`, `MailHog`, `Infisical`, and `Observability`.
Executable scripts are in matching `Scripts/Bash` and `Scripts/PowerShell` trees. You can start only
the required components or the prepared stacks, such as Kafka with Kafka UI, Redis with
RedisInsight, and the complete observability stack.

For a first-time setup on Ubuntu, run the bootstrap from the repository root:

```bash
./Scripts/Bash/setup-project.sh
```

On Windows with PowerShell, run:

```powershell
.\Scripts\PowerShell\setup-project.ps1
```

The script validates the required host tools, pulls and starts all Docker infrastructure,
restores the backend and frontend dependencies, builds both applications, and applies all
database migrations. It does not install host tools or delete existing databases, topics,
or Docker volumes. Infisical and the observability stack are included by default. All
containers are grouped as the single `flowchat` Docker Compose project in Docker Engine.
When upgrading from the earlier bootstrap scripts, setup safely replaces their legacy
Compose groups while retaining the existing Docker volumes.

Optional switches:

```powershell
.\Scripts\PowerShell\setup-project.ps1 -SkipPull
.\Scripts\PowerShell\setup-project.ps1 -SkipBuild
.\Scripts\PowerShell\setup-project.ps1 -SkipMigrations
.\Scripts\PowerShell\setup-project.ps1 -SkipInfisical
.\Scripts\PowerShell\setup-project.ps1 -SkipObservability
```

The Bash equivalents use lower-case, hyphenated options, for example
`./Scripts/Bash/setup-project.sh --skip-pull --skip-build`.

If the ASP.NET Core development certificate is not trusted, the script prints the
one-time `dotnet dev-certs https --trust` command. Once setup finishes, open
`FlowChat.code-workspace` and start the `All Services` debug compound.

Individual bootstrap scripts remain available when only one infrastructure component is
needed. After starting PostgreSQL manually, migrations for all registered services can be
applied with `./Scripts/Bash/PostgreSQL/migrate-all.sh` or
`./Scripts/PowerShell/PostgreSQL/migrate-all.ps1`.

The backend can be started separately from each service solution or by using compounds
in `FlowChat.code-workspace`. The default public gateway address in the development
environment is `https://localhost:7270`.

Start the client with:

```powershell
cd ReactClient
npm install
npm run dev
```

By default, the client runs at `http://localhost:5173` and expects GatewayService at
`https://localhost:7270`. These addresses can be changed using
`VITE_GATEWAY_API_URL` and `VITE_REALTIME_API_URL`.

Service configuration is stored in `appsettings.json` and
`appsettings.Development.json`. Secrets and local values should not be committed to the
repository; they can be supplied through environment variables, local configuration
files, or Infisical.
