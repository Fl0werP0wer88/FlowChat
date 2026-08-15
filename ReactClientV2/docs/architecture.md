# Architecture

## Application scope

ReactClientV2 is a clean migration of FlowChat's frontend onto React 19, TypeScript, and Vite 7. The current release owns:

- login by email or FriendlyUserId;
- registration without automatic login;
- email verification;
- refresh-cookie session restoration and access-token lifecycle;
- guest and protected route behavior;
- a protected placeholder for the future chat migration.

Chat data, SignalR, contacts, conversations, and profiles are intentionally outside the current boundary.

## Dependency direction

Code follows a one-way dependency model:

```text
shared modules -> features -> app
```

The arrow means “may be consumed by.” Shared code knows nothing about features or the application shell. Features consume shared code but remain isolated from one another. The application shell composes both.

ESLint enforces these constraints:

- `src/app` may import features and shared modules.
- `src/features/<name>` may import shared modules and files from the same feature.
- A feature must not import another feature or `src/app`.
- Shared modules must not import features or `src/app`.
- Import cycles are forbidden.

Cross-feature workflows belong in `src/app` or in a new explicitly shared abstraction when the behavior is genuinely feature-independent.

## Project structure

```text
src/
├── app/          routing, route guards, route modules, provider composition
├── components/   shared UI, layouts, brand, and application-wide fallbacks
├── config/       validated environment and static route configuration
├── features/     self-contained business capabilities
├── hooks/        reusable application-independent React hooks
├── lib/          configured infrastructure such as Axios, Query Client, and sessions
├── stores/       minimal global client state
├── testing/      shared test setup, MSW handlers, fixtures, and render utilities
├── types/        types shared by more than one feature or infrastructure module
└── utils/        small pure shared helpers
```

A feature contains only the folders it needs. The current pattern is:

```text
features/<feature>/
├── api/          endpoint declarations, schemas, and query/mutation integration
├── components/   feature-owned UI and colocated integration tests
├── hooks/        optional feature-specific orchestration
├── stores/       optional feature-owned client state
├── types/        optional feature-local types
└── utils/        optional feature-local pure helpers
```

Import concrete modules directly. Broad barrel files are avoided because they obscure dependencies and can weaken tree shaking.

## Application layer

`src/app/provider.tsx` composes global providers and fallbacks. Provider order is deliberate: Suspense and the global error boundary surround TanStack Query, notifications, and session bootstrap.

`src/app/router.tsx` owns route registration and lazy route modules. Route guards wait until session bootstrap finishes so the application does not briefly render the wrong page.

Routing behavior:

- `/login` and `/register` are guest-only.
- `/email-verification` is available regardless of session state because verification links may be opened independently.
- `/chat` is protected.
- anonymous protected navigation carries a safe local `redirectTo` value to login;
- `/` and unknown paths resolve according to authentication state.

## State ownership

State is split by responsibility:

- Component state handles temporary, local presentation behavior.
- React Hook Form owns form state and Zod owns form validation.
- TanStack Query owns server cache, request lifecycle, and request deduplication.
- Zustand owns the small in-memory authentication session shared across routing and infrastructure.
- React Router owns URL search parameters and navigation state.

Do not mirror TanStack Query data in Zustand. Do not move local state into a global store until multiple independent consumers require it.

## Adding a feature

1. Define the user behavior and backend boundary.
2. Add request and response schemas and the endpoint declaration inside the feature.
3. Build feature components with accessible shared primitives.
4. Add behavior-focused integration tests with MSW.
5. Compose the feature from an app route or another app-level integration point.
6. Add a lazy route when the feature introduces a new screen.
7. Update these documents if the change introduces a new architectural rule.
