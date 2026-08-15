# Development standards

## Toolchain

ReactClientV2 uses:

- React 19;
- TypeScript in strict mode;
- Vite 7;
- Tailwind CSS 4;
- ESLint 9 flat configuration;
- Prettier;
- npm with a committed `package-lock.json`.

Use the dependency versions declared in `package.json`. The upstream Bulletproof React versions are not constraints for this project.

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

Use `npm run format` to apply formatting and `npm run format:check` to verify it without writes.

## TypeScript

- Keep strict checking enabled.
- Define precise boundary types and validate runtime data separately.
- Avoid `any`, unchecked casts, and non-null assertions that hide uncertain data.
- Prefer discriminated states or schemas when a workflow has meaningful alternatives.
- Change types first during a refactor, then resolve compiler errors systematically.

## Imports and dependencies

- Use `@/` absolute imports for modules under `src`.
- Use relative imports only for tightly related sibling modules when it makes ownership clearer.
- Preserve import ordering enforced by ESLint.
- Do not introduce cycles.
- Do not import across features.
- Do not create catch-all barrel exports.

The dependency direction and exceptions are defined in `eslint.config.js` and explained in [Architecture](architecture.md).

## Naming

- Files and folders: kebab-case.
- React components and exported component types: PascalCase.
- Functions, variables, and hooks: camelCase.
- Hooks begin with `use`.
- Tests use `.test.ts` or `.test.tsx`; Playwright specifications use `.spec.ts`.

Prefer names that describe the domain behavior rather than the implementation library.

## Configuration

Environment variables are parsed once in `src/config/env.ts` with Zod. Feature code consumes the exported configuration and must not access `import.meta.env` directly.

Only variables prefixed with `VITE_` reach browser code. They are public configuration, never secrets.

## Formatting and linting

Do not disable lint rules merely to land a change. Fix the dependency or accessibility issue. A narrow disable is acceptable only when the rule is demonstrably incorrect and the reason is documented beside it.

Before review, the repository must pass lint, type checking, tests, production build, and E2E tests.

## Comments and documentation

Comments explain non-obvious reasons, especially security, compatibility, and deliberate tradeoffs. Do not restate self-explanatory code.

Update documentation in the same change when modifying:

- dependency boundaries;
- backend contracts;
- session or security behavior;
- development commands or ports;
- testing strategy;
- deployment assumptions.

Keep `AGENTS.md` and `CLAUDE.md` identical.

## Change workflow

1. Confirm the feature boundary and affected contracts.
2. Inspect existing patterns before adding an abstraction.
3. Implement the smallest complete behavior.
4. Add or update behavior-focused tests.
5. Run the required checks.
6. Review the final diff for accidental backend or `ReactClient` changes.
7. Update documentation when an enduring project decision changed.
