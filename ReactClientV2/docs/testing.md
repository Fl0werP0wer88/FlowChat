# Testing

## Strategy

Confidence comes primarily from integration tests and a small set of critical E2E journeys. Unit tests remain useful for isolated shared logic with meaningful branching.

Tests should describe behavior visible to a user or an integration boundary. Avoid assertions against component internals, hook implementation details, or incidental DOM structure.

## Integration tests

Vitest, Testing Library, and MSW cover:

- guest and authenticated routing;
- session bootstrap;
- email and FriendlyUserId login;
- invalid credentials;
- registration validation, normalization, payloads, and login prefill;
- scheduled refresh and refresh after `401`;
- concurrent refresh deduplication;
- logout and cache cleanup;
- all email-verification states.

Prefer real user interactions through Testing Library. Query by role, label, accessible name, or visible text. Use MSW at the HTTP boundary instead of mocking Axios or fetch.

Shared setup belongs in `src/testing`. Feature tests remain colocated in `__tests__` beside the relevant feature or application behavior.

## Strict Mode

The test renderer uses the same relevant provider behavior as the application. Requests triggered by mounting must be designed to tolerate React Strict Mode. TanStack Query keys and the single-flight refresh promise provide deduplication; tests should assert externally visible request counts for these critical paths.

## E2E tests

Playwright exercises critical browser journeys:

- protected chat redirect, login, and logout;
- registration followed by a prefilled login form;
- email-verification success, failure, and retry.

The E2E environment is intentionally isolated:

| Service          |    Port |
| ---------------- | ------: |
| Vite application |  `5174` |
| mock API         | `18080` |

This does not change normal development: V2 uses `5173` and the real Gateway URL by default.

The mock server is test infrastructure, not a second application contract. Keep its handlers aligned with the Gateway shapes consumed by the frontend.

## Accessibility and responsive checks

Critical form tests assert labels, validation feedback, disabled submission behavior, and reachable actions. E2E journeys provide real-browser keyboard and layout confidence. When changing layout or shared controls, manually inspect at least one narrow and one desktop viewport and verify reduced-motion behavior when applicable.

## Required checks

Run from `ReactClientV2`:

```bash
npm run lint
npm run check-types
npm test
npm run build
npm run test:e2e
```

During development, use `npm run test:watch` for focused feedback. Do not mark a change complete with a failing check unless the user explicitly accepts the known failure.

## What to test

Add tests for:

- new user-visible behavior;
- request shape or response parsing changes;
- authentication and route-guard changes;
- validation rules;
- error, retry, and loading states;
- regressions fixed by the change.

Avoid low-value tests for passive type declarations, static configuration constants, or visual class strings with no behavioral contract.
