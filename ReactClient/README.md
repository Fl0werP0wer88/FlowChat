# ReactClient

FlowChat Web UI (React + TypeScript + Vite).

## Requirements

- Node.js 20+
- AuthService running (default: `https://localhost:7236`)

## Configure

Copy `.env.example` to `.env` and adjust if needed:

```bash
VITE_AUTH_API_URL=https://localhost:7236
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
