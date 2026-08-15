# Deployment

## Build

Create the production assets with:

```bash
npm ci
npm run build
```

The output is written to `dist`. The build includes strict type checking before Vite bundles the application.

## Environment

`VITE_GATEWAY_API_URL` is the only current runtime build variable. It must be an absolute URL and is validated when the application starts.

```text
VITE_GATEWAY_API_URL=https://gateway.example.com
```

The development default is `https://localhost:7270`. Values prefixed with `VITE_` are embedded in browser assets and must never contain secrets.

## Ports

- normal Vite development: `5173`;
- local preview: `4173`;
- E2E-only Vite server: `5174`;
- E2E-only mock API: `18080`.

Keep normal development on `5173` while Gateway CORS and email-verification links target that origin.

## Hosting

Serve `dist` as static assets through a CDN or static web host. Configure an SPA fallback so unknown application paths resolve to `index.html`; otherwise direct navigation to `/login`, `/email-verification`, or `/chat` will return a host-level 404.

`public/_redirects` provides the fallback format used by compatible hosts. Other platforms need an equivalent rewrite rule.

Serve production traffic over HTTPS. Configure the Gateway's allowed origin and secure refresh cookie for the deployed frontend origin. Frontend deployment alone cannot correct an incompatible CORS or cookie policy.

## Caching

- Cache fingerprinted Vite assets for a long duration with immutable semantics.
- Serve `index.html` with short caching or revalidation so it can reference the current asset hashes.
- Do not cache authentication or verification API responses at the CDN.

## Observability

If production error tracking is introduced, keep it behind an application-level adapter and upload matching source maps securely during deployment. Do not expose source-map upload credentials through `VITE_` variables or ship them in browser assets.

## Release verification

Before deployment:

1. Run lint, type checks, integration tests, production build, and E2E tests.
2. Verify the production Gateway URL.
3. Verify the deployed origin is allowed by Gateway CORS.
4. Verify refresh-cookie attributes work for the production topology.
5. Open login, registration, verification, and a protected-route deep link directly.
6. Confirm source maps and error telemetry do not include credentials or tokens.
