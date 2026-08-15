# ReactClientV2 Agent Instructions

## Scope

These instructions apply to `ReactClientV2`. The repository-root instructions also apply and take precedence when they are stricter.

ReactClientV2 is the React 19, TypeScript, and Vite 7 client for FlowChat. The current migration scope includes authentication, registration, email verification, session lifecycle, and a protected chat placeholder.

- Keep all user-facing copy, validation messages, notifications, placeholders, document titles, and accessibility labels in English.
- Do not modify the backend or `ReactClient` while working on ReactClientV2 unless the user explicitly expands the scope.
- Do not add chat APIs, SignalR, contacts, conversations, or profile behavior until that migration stage is requested.
- Keep normal development on port `5173`; Gateway CORS and verification links depend on it.

## Architecture

Follow the feature-first architecture documented in `docs/architecture.md`:

```text
shared modules -> features -> app
```

- `src/app` owns routing, route guards, provider composition, and application-level integration.
- `src/features/<feature>` owns feature API declarations, validation, components, hooks, and feature-only utilities.
- `src/components`, `config`, `hooks`, `lib`, `stores`, `types`, and `utils` are shared modules and must not import from `features` or `app`.
- Features may import shared modules but must not import from `app` or another feature.
- Compose multiple features in `app`; never create cross-feature dependencies to avoid app-level composition.
- Import concrete files directly. Do not add barrel files unless they provide a deliberate public boundary with a demonstrated need.
- Colocate tests and feature-specific code with the behavior they cover.

ESLint enforces the dependency direction, cross-feature isolation, cycles, accessibility rules, import order, and kebab-case filenames. Do not bypass these rules with relative-path tricks or disable comments.

## API and authentication

- Use the single Axios instance in `src/lib/api-client.ts` for all Gateway requests.
- Keep `withCredentials` enabled and derive the base URL only from validated configuration in `src/config/env.ts`.
- Validate untrusted HTTP responses at runtime with Zod before using them.
- Normalize server failures to `ApiError`; UI code should not depend on Axios error shapes.
- Keep endpoint declarations inside the feature that owns them. Shared authentication/session infrastructure remains in `src/lib`.

The backend contracts are fixed unless a separately authorized backend change says otherwise:

- form-encoded `POST /api/users/login`
- form-encoded `POST /api/users/refresh-token`
- `POST /api/users/logout`
- JSON `PUT /api/users`
- JSON `POST /api/userprofiles/email-verification/confirm`

Authentication rules are non-negotiable:

- Keep the access token only in the in-memory Zustand store. Never persist it in local storage, session storage, IndexedDB, or a JavaScript-readable cookie.
- Restore sessions through the existing HttpOnly refresh cookie before resolving protected or guest routing.
- Attach Bearer tokens in the Axios request interceptor.
- Refresh at most once for concurrent callers and retry a failed request at most once after a `401`.
- Login, refresh, and logout requests must not enter the refresh loop.
- Decode JWT claims only for display and refresh scheduling. The server remains authoritative for authentication and authorization.
- Clear both the in-memory session and TanStack Query cache during logout, even when the logout request fails.
- Never expose detailed authentication failures that allow account enumeration.

See `docs/api-and-authentication.md` for the complete flow.

## State and forms

- Use component state for local UI behavior.
- Use React Hook Form with Zod for forms and validation.
- Use TanStack Query for server state, request deduplication, and mutations where caching or lifecycle management is useful.
- Use Zustand only for genuinely shared client state. The authentication store must stay small and memory-only.
- Keep URL state in React Router search parameters or navigation state.
- Do not duplicate server data in Zustand.

FriendlyUserId values must be trimmed, lowercased, and validated against the backend-compatible rules before submission. Prevent duplicate submissions and preserve accessible error focus behavior.

## Routing and errors

- Define routes in `src/app/router.tsx` and lazy-load route modules.
- Anonymous users visiting protected routes go to login with a safe `redirectTo` value.
- Authenticated users cannot revisit login or registration routes.
- Root and wildcard routes redirect according to session state.
- Keep the global error boundary in the application provider and add localized route/feature error states when recovery can happen locally.
- Show actionable, English error messages and notifications without leaking tokens or sensitive response details.

## Components and styling

- Prefer semantic HTML, visible labels, keyboard support, predictable focus, and `aria-invalid`/error associations for form controls.
- Shared UI primitives belong in `src/components/ui`; feature-specific composition belongs in its feature.
- Extract a component when a piece of UI has a clear responsibility. Avoid large components with nested render helpers and components with excessive prop surfaces.
- Use Tailwind CSS 4 and the existing visual language: bright surfaces, deep navy brand areas, a single cobalt accent, and restrained conversation-flow motifs.
- Preserve responsive behavior and full `prefers-reduced-motion` support.
- Reuse an existing component only after a real repeated pattern exists; avoid speculative abstractions.

## Testing

Prefer behavior-focused integration tests over implementation-detail tests.

- Vitest and Testing Library cover components, routing, session behavior, and shared logic.
- MSW handles integration-test HTTP boundaries; do not mock Axios or fetch directly when an MSW request expresses the behavior better.
- Playwright covers critical browser journeys against the dedicated mock API.
- Keep E2E isolated from normal development. The current test harness uses app port `5174` and mock API port `18080`; production and development still use `5173` and the Gateway.
- Add regression coverage for changed behavior, including error and retry paths where applicable.
- Verify Strict Mode does not produce duplicate externally visible requests.

Before completing a change, run:

```bash
npm run lint
npm run check-types
npm test
npm run build
npm run test:e2e
```

Report what passed, what failed, and any intentionally untested risk.

## Project standards

- Use strict TypeScript and the `@/` alias for imports from `src`.
- Use kebab-case filenames and folders, PascalCase component names, and camelCase variables and functions.
- Preserve direct, explicit types at API and feature boundaries; do not use `any` to suppress design problems.
- Add comments only when they explain a non-obvious reason, security constraint, or compatibility decision.
- Keep configuration validated and centralized; do not read `import.meta.env` throughout feature code.
- Update the relevant file in `docs` whenever an architectural rule, API contract, test workflow, or deployment assumption changes.
- Keep `AGENTS.md` and `CLAUDE.md` synchronized.

## Reference

The architecture is adapted from Bulletproof React's Vite application, pinned for reference at commit `9506629ed003a561c6627735480cce4994244bb4`. ReactClientV2 documentation is the source of truth for this project; upstream guidance is not automatically binding.
