# Analysis Agent Prompt Template

This template is used by the orchestrator to construct the analysis agent's task.
The orchestrator should replace `{ServiceName}` with the actual service name.

---

## Prompt

```
Analyze test coverage gaps for the {ServiceName} service in the FlowChat project.

## Instructions

### 1. Scan Source Files

Use Glob to find all .cs files in {ServiceName}/src/ recursively.
Exclude: obj/, bin/, GlobalUsings.cs, *.Designer.cs, Migrations/

Categorize each file by layer based on the project name:
- Domain: FlowChat.{ServiceName}.Domain
- Application: FlowChat.{ServiceName}.Application
- Infrastructure: FlowChat.{ServiceName}.Infrastructure
- Persistence: FlowChat.{ServiceName}.Persistence
- API: FlowChat.{ServiceName}.API
- Workers: FlowChat.{ServiceName}.OutboxPublisher, FlowChat.{ServiceName}.Consumers

### 2. Scan Existing Tests

Use Glob to find all *Tests.cs files in {ServiceName}/tests/ recursively.
Exclude: obj/, bin/, GlobalUsings.cs

Categorize by project:
- UnitTests: FlowChat.{ServiceName}.UnitTests
- IntegrationTests: FlowChat.{ServiceName}.IntegrationTests
- AATs: FlowChat.{ServiceName}.AATs

### 3. Match Source to Tests

For each source file, check if a corresponding test exists.
Matching rules:
- {ClassName}.cs → {ClassName}Tests.cs
- Path under src layer → mirrored path under tests

### 4. Classify Each Source File

For each source file, assign ONE status:

COVERED — a test file already exists for this class
MISSING_UNIT — needs unit tests (Domain entities/VOs, Application handlers, Infrastructure services)
MISSING_INTEGRATION — needs integration tests (Persistence repos/UoW, DI registration, Kafka config)
MISSING_BOTH — needs both unit and integration tests
SKIP — does not need tests, with reason:
  - Simple DTO/record with no logic
  - Interface definition only
  - Constants/enums with no computed values
  - ServiceRegistration files (tested via integration tests)
  - GlobalUsings, Program.cs boilerplate
  - Migrations
  - Marker types

### 5. Determine What to Test

For files that need tests, briefly READ the source file to understand:
- What public methods exist
- What the class does
- What scenarios should be tested (happy path, error cases, edge cases)

Write a one-line DESCRIPTION of what to test.

### 6. Assign Priority

- Priority 1: Domain — aggregate roots, entities, value objects
- Priority 2: Application — command/query handlers
- Priority 3: Application — domain event handlers, event mapping profiles
- Priority 4: Infrastructure — services (JWT, settings managers)
- Priority 5: Persistence — repositories, Unit of Work, interceptors
- Priority 6: API — controllers
- Priority 7: Workers — outbox publisher, consumers, Kafka subscribers

### 7. Output Format

Return EXACTLY this format (parseable by the orchestrator):

===ANALYSIS_START===
SERVICE: {ServiceName}

GAPS:
---GAP---
SOURCE: {relative/path/to/source.cs}
TEST_TYPE: unit|integration
TARGET: {relative/path/to/target/Tests.cs}
LAYER: Domain|Application|Infrastructure|Persistence|API|Workers
PRIORITY: {1-7}
CLASS_NAME: {ClassName}
DESCRIPTION: {one-line description of what tests should cover}
---GAP---
{next gap...}

COVERED:
---COVERED---
SOURCE: {relative/path/to/source.cs}
TEST: {relative/path/to/existing/test.cs}
---COVERED---
{next covered...}

SKIPPED:
---SKIPPED---
SOURCE: {relative/path/to/source.cs}
REASON: {why this file doesn't need tests}
---SKIPPED---
{next skipped...}

SUMMARY:
TOTAL_SOURCE_FILES: {N}
COVERED: {N}
MISSING: {N}
SKIPPED: {N}
===ANALYSIS_END===
```
