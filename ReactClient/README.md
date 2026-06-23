# ReactClient

FlowChat Web UI (React + TypeScript + Vite).

## Requirements

- Node.js 20+
- GatewayService running (default: `https://localhost:7270`)

## Configure

Copy `.env.example` to `.env` and adjust if needed:

```bash
VITE_GATEWAY_API_URL=https://localhost:7270
VITE_REALTIME_API_URL=https://localhost:7270
```

## Run

```bash
npm install
npm run dev
```

## Build

```bash
npm run build
```

## Type Check

```bash
npx tsc --noEmit
```
