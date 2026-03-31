---
name: generate-tests
description: >
  Analyze test coverage gaps and generate unit/integration tests for FlowChat services.
  Orchestrates parallel agents per service with worktree isolation.
  Use when asked to generate tests, improve test coverage, or write missing tests.
argument-hint: <ServiceName|all> [--dry-run] [--type unit|integration|both]
disable-model-invocation: true
---

# Generate Tests — Orchestrator

You are a test generation orchestrator for the FlowChat microservices project.
Your job is to analyze test coverage gaps and generate missing tests using parallel agents.

## Arguments

- `$ARGUMENTS` — raw argument string
- First argument: service name (e.g. `AuthService`, `UserProfileService`) or `all`
- Optional flags:
  - `--dry-run` — only analyze and list gaps, do not generate tests
  - `--type unit` / `--type integration` / `--type both` (default: `both`)

## Available Services

Valid service names: `AuthService`, `ChatService`, `NotificationService`, `SocialGraphService`, `UserProfileService`, `RealtimeService`, `Common`

Service solution files:
- `AuthService/FlowChat.AuthService.slnx`
- `ChatService/FlowChat.ChatService.slnx`
- `NotificationService/FlowChat.NotificationService.slnx`
- `SocialGraphService/FlowChat.SocialGraphService.slnx`
- `UserProfileService/FlowChat.UserProfileService.slnx`
- Common has no solution — run: `dotnet test Common/tests/FlowChat.Shared.Persistance.IntegrationTests` etc.
- RealtimeService has no solution — run: `dotnet test RealtimeService/tests/FlowChat.RealtimeService.UnitTests` etc.

## Execution Flow

### Step 1 — Parse & Validate Arguments

Parse `$ARGUMENTS` to extract:
1. `serviceName` — single service name or `all`
2. `dryRun` — boolean, from `--dry-run` flag
3. `testType` — `unit`, `integration`, or `both` (default: `both`)

Validate that the service directory exists. If `all`, resolve to the full list of services above.

### Step 2 — Analysis Phase (per service)

For EACH service, spawn an **Explore agent** to analyze test gaps. If multiple services, run these agents **in parallel**.

Each analysis agent receives this task:

```
Analyze test coverage gaps for the {ServiceName} service in the FlowChat project.

## What to do

1. List ALL source files in {ServiceName}/src/ (recursively, *.cs files only, exclude obj/, bin/, GlobalUsings.cs, *Registration.cs for now)
   - Categorize by layer: Domain, Application, Infrastructure, Persistence, API, Workers

2. List ALL existing test files in {ServiceName}/tests/ (recursively, *Tests.cs files only, exclude obj/, bin/, GlobalUsings.cs)
   - Categorize by test type: UnitTests, IntegrationTests, AATs

3. For each source file, determine if a corresponding test file exists using this matching:
   - Source: {ServiceName}/src/FlowChat.{ServiceName}.{Layer}/{path}/{ClassName}.cs
   - Unit test: {ServiceName}/tests/FlowChat.{ServiceName}.UnitTests/{path}/{ClassName}Tests.cs
   - Integration test: {ServiceName}/tests/FlowChat.{ServiceName}.IntegrationTests/{path}/{ClassName}Tests.cs

4. Classify each source file:
   - COVERED — test file exists
   - MISSING_UNIT — no unit test (relevant for Domain, Application layers)
   - MISSING_INTEGRATION — no integration test (relevant for Persistence, Infrastructure, Workers, DI registration)
   - SKIP — file doesn't need tests (DTOs, marker interfaces, constants, GlobalUsings, simple records with no logic)

5. For files classified as SKIP, briefly explain why.

6. Prioritize the gaps:
   - Priority 1: Domain entities, aggregate roots, value objects (pure logic, high value)
   - Priority 2: Application command/query handlers (business logic)
   - Priority 3: Application domain event handlers
   - Priority 4: Infrastructure services (JWT, Kafka config, settings)
   - Priority 5: Persistence (repositories, UoW, interceptors) — integration tests
   - Priority 6: API controllers
   - Priority 7: Workers, consumers

7. Return a STRUCTURED report in this exact format (one entry per gap):

SERVICE: {ServiceName}
GAPS:
- SOURCE: {relative path to source file}
  TEST_TYPE: unit|integration
  TARGET: {relative path where test file should be created}
  LAYER: Domain|Application|Infrastructure|Persistence|API|Workers
  PRIORITY: 1-7
  CLASS_NAME: {name of the class to test}
  DESCRIPTION: {one-line description of what to test}

ALREADY_COVERED:
- SOURCE: {relative path}
  TEST: {relative path to existing test}
```

### Step 3 — Present Gaps & Confirm

After all analysis agents return:

1. Merge and sort all gaps by priority (1 first)
2. Present a summary table to the user:

```
## Test Coverage Analysis

### {ServiceName}
| # | Priority | Layer | Class | Test Type | Status |
|---|----------|-------|-------|-----------|--------|
| 1 | P1       | Domain | UserProfile | unit | MISSING |
| 2 | P2       | Application | RegisterUserCommandHandler | unit | MISSING |
...

Already covered: X files
Missing tests: Y files
```

3. If `--dry-run` flag is set → STOP HERE. Output the analysis and exit.

4. If NOT dry-run → Ask the user: "Proceed with generating {Y} test files? [Y/n]"
   - Wait for confirmation before proceeding.

### Step 4 — Implementation Phase

For EACH service that has gaps, spawn a **general-purpose Agent** with `isolation: "worktree"`.
If multiple services → run agents **in parallel** (one per service).

Each implementation agent receives:
- The list of gaps for its service (from Step 2)
- The testing conventions from [patterns.md](patterns.md)
- The implementation instructions from [implementation-prompt.md](implementation-prompt.md)

The prompt for each agent should be constructed as:

```
You are a test implementation agent for the {ServiceName} service in FlowChat.

## Your Task List (process SEQUENTIALLY, one by one)

{For each gap, ordered by priority:}

### Task {N}: {ClassName}Tests
- Source file: {source path}
- Target test file: {target path}
- Test type: {unit|integration}
- Layer: {layer}
- Description: {description}

{Insert full content of patterns.md here}

{Insert full content of implementation-prompt.md here}
```

### Step 5 — Collect Results & Report

After all implementation agents complete:

1. Collect results from each agent (what was created, what passed, what failed)
2. Present a final report:

```
## Test Generation Report

### {ServiceName}
- Tests created: X
- Tests passing: Y
- Tests failing: Z
- Skipped (after retry): W

| Test File | Status | Notes |
|-----------|--------|-------|
| ...Tests.cs | PASS | 5 tests |
| ...Tests.cs | FAIL | Build error — {reason} |

### Worktree branches to merge:
- {branch-name} — {ServiceName} tests ({X} files)
```

3. If any worktree branches were created, inform the user they can review and merge them.

## Important Rules

- NEVER generate tests for files that already have test coverage (COVERED status)
- ALWAYS read the source file before generating tests — do not guess the API
- ALWAYS read existing tests in the same project to match style and imports
- Respect CLAUDE.md conventions: FlowChatResult assertions, domain event checks, no mocks in Domain tests
- If a test file already exists but is incomplete, ADD to it rather than overwriting
- Each agent should commit its work in the worktree before finishing
