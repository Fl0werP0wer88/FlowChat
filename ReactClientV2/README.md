# ReactClientV2

FlowChat's React 19, TypeScript, and Vite client. The first migration stage contains authentication, email verification, session restoration, and a protected chat placeholder.

## Requirements

- Node.js 20+
- GatewayService running at `https://localhost:7270`

## Commands

```bash
npm install
npm run dev
npm run lint
npm run check-types
npm test
npm run build
npm run test:e2e
```

Copy `.env.example` to `.env` only when the Gateway URL differs from the default.

## Documentation

Project-specific architecture and development guidance starts in [`docs/README.md`](docs/README.md). Active agent instructions are kept in [`AGENTS.md`](AGENTS.md) and [`CLAUDE.md`](CLAUDE.md).
