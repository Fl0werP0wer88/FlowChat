# API and authentication

## Gateway client

All HTTP traffic uses the configured Axios singleton in `src/lib/api-client.ts`.

- `baseURL` comes from the Zod-validated `VITE_GATEWAY_API_URL` value.
- `withCredentials` is always enabled so the browser can send the Gateway's HttpOnly refresh cookie.
- The request interceptor adds the current in-memory access token as a Bearer token.
- The response interceptor normalizes failures and may perform one refresh-and-retry cycle after a `401`.

Feature code declares endpoints in its own `api` folder. A declaration includes the request shape, runtime response validation when a response body is consumed, the fetcher, and a TanStack Query hook when query caching or mutation lifecycle is useful.

## Backend contracts

ReactClientV2 consumes the existing contracts without backend changes:

| Purpose       | Method and path                                     | Encoding         |
| ------------- | --------------------------------------------------- | ---------------- |
| Login         | `POST /api/users/login`                             | form URL encoded |
| Refresh       | `POST /api/users/refresh-token`                     | form URL encoded |
| Logout        | `POST /api/users/logout`                            | no request body  |
| Registration  | `PUT /api/users`                                    | JSON             |
| Confirm email | `POST /api/userprofiles/email-verification/confirm` | JSON             |

The API URL defaults to `https://localhost:7270` in development. Configure a different Gateway only through `VITE_GATEWAY_API_URL`.

## Runtime validation

TypeScript types disappear at runtime, so every untrusted response used by the application is parsed with Zod. Invalid success responses are failures; do not cast them into the expected type.

`ApiError` is the UI-facing error model. The normalization layer understands:

- RFC Problem Details responses;
- OpenIddict `error` and `error_description` responses;
- plain server messages;
- connectivity and unexpected client failures.

Feature components render normalized, actionable messages and must not inspect Axios internals.

## Session lifecycle

The access token exists only in the in-memory Zustand store. It is never written to browser persistence. The refresh token remains in the backend-managed HttpOnly cookie and is not readable by React code.

```text
application start
    -> POST refresh-token with the HttpOnly cookie
    -> validate the token response
    -> establish the in-memory session
    -> resolve guest/protected routing
```

While bootstrap is pending, the application renders a loading state rather than making an authentication guess.

JWT decoding is limited to display fields and refresh scheduling. Claims decoded in the browser are not proof of authorization; protected resources must always be enforced by the server.

## Refresh behavior

Session refresh happens:

- during application bootstrap;
- one minute before the access token expires;
- after an eligible API request receives `401`.

Only one refresh request may be active. Concurrent callers share the same promise. After a successful refresh, the original failed request is retried once with the new token.

Login, refresh, and logout opt out of interceptor refresh handling. This prevents recursive refresh loops. A failed refresh clears the local session and leaves route guards to return the user to login.

## Login and registration

Login accepts either an email address or FriendlyUserId. Authentication failures use a generic message so the interface does not disclose whether an account exists.

Registration:

- requires a valid email;
- trims and lowercases FriendlyUserId before validation and submission;
- requires a password of at least eight characters;
- accepts optional first name, last name, and organization;
- prevents duplicate submission;
- returns to login after success and prefills the registered email through navigation state;
- never establishes a session automatically.

## Email verification

The verification route reads `token` from the URL and uses TanStack Query to deduplicate confirmation in React Strict Mode. It represents pending, success, error, missing-token, and manual retry states. Tokens must not be logged, persisted, or included in notification diagnostics.

## Logout

Logout calls `/api/users/logout`. Local cleanup runs even if the network request fails:

- clear the in-memory session;
- clear TanStack Query cache;
- allow protected routing to return to login.

## Security rules

- Never persist access or refresh tokens in JavaScript-readable storage.
- Never use decoded client claims as authoritative access control.
- Never render arbitrary HTML without an explicit sanitization boundary.
- Never log credentials, tokens, authorization headers, or full authentication responses.
- Keep `redirectTo` values local to this application to prevent open redirects.
- Prefer generic authentication errors where detailed messages could enable account enumeration.
