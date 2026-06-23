---
name: generate-tests
description: >
  Analyze test coverage gaps and generate unit/integration tests for FlowChat services.
  Uses the repo-local PowerShell orchestrator at scripts/generate-tests.ps1 and keeps
  runtime assets under .codex/testgen/ and .codex/worktrees/.
argument-hint: <ServiceName|all> [--dry-run] [--type unit|integration|both] [--resume <run-id>] [--max-retries <n>] [--max-parallel-services <n>] [--cleanup-worktrees]
disable-model-invocation: true
---

# Generate Tests - Codex Front

Use the repo-local orchestrator instead of expanding the full workflow in chat.

## Entry point

Run:

```powershell
powershell -ExecutionPolicy Bypass -File scripts/generate-tests.ps1 $ARGUMENTS
```

## Expected behavior

The wrapper script is responsible for:

1. Parsing the CLI arguments
2. Running preflight checks
3. Creating `.codex/testgen/runs/<run-id>/`
4. Using `analysis-prompt.md`, `implementation-prompt.md`, and `patterns.md`
5. Spawning analysis and implementation agents with `codex exec`
6. Writing `manifest.json`, `analysis.<service>.json`, `tasks.<service>.json`, `results.<service>.json`, and `summary.md`
7. Reporting the final summary back to the user

## Interaction model

- Prefer a single service by default
- Use `all` only when the user explicitly requests it
- Use `--dry-run` when the user asks only for analysis or backlog generation
- When the command completes, summarize the key results from `summary.md`
- If the run fails, surface the failing phase and point the user to the run directory
