# ReactClientV2 documentation

This documentation adapts the ideas from Bulletproof React to FlowChat's actual React 19 and Vite 7 client. It describes decisions already adopted by the project rather than every option presented by the upstream guide.

## Guides

- [Architecture](architecture.md) — application scope, dependency direction, routing, project structure, and state ownership.
- [API and authentication](api-and-authentication.md) — Gateway contracts, runtime validation, session restoration, refresh, logout, and error handling.
- [Components and styling](components-and-styling.md) — component boundaries, accessibility, visual language, responsive behavior, and performance.
- [Development standards](development-standards.md) — TypeScript, imports, naming, linting, formatting, and change workflow.
- [Testing](testing.md) — Vitest, Testing Library, MSW, Playwright, test scope, and required checks.
- [Deployment](deployment.md) — environment configuration, ports, build output, SPA hosting, and security considerations.
- [Resources and provenance](resources.md) — upstream revision, license, and selected primary resources.

The active coding instructions are in [`../AGENTS.md`](../AGENTS.md). If documentation and code diverge, update both as part of the change.
