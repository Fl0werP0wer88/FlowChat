# Resources and provenance

## Architecture source

ReactClientV2 adapts the React Vite architecture from [Bulletproof React](https://github.com/alan2207/bulletproof-react) by Alan Alickovic.

The local reference used for the initial architecture and this documentation was:

- repository: `https://github.com/alan2207/bulletproof-react.git`;
- commit: `9506629ed003a561c6627735480cce4994244bb4`;
- license: MIT, Copyright (c) 2024 Alan Alickovic.

The upstream documents informed the dependency direction, feature isolation, API declaration pattern, component colocation, state categories, route-level splitting, error boundaries, and integration-first testing strategy.

This directory is an adaptation, not a verbatim vendored copy. ReactClientV2 uses newer dependencies and FlowChat-specific authentication contracts. The local documentation and code are authoritative for this project.

## Primary references

- [React documentation](https://react.dev/)
- [TypeScript documentation](https://www.typescriptlang.org/docs/)
- [Vite documentation](https://vite.dev/guide/)
- [React Router documentation](https://reactrouter.com/)
- [TanStack Query documentation](https://tanstack.com/query/latest)
- [Zustand documentation](https://zustand.docs.pmnd.rs/)
- [React Hook Form documentation](https://react-hook-form.com/)
- [Zod documentation](https://zod.dev/)
- [Axios documentation](https://axios-http.com/docs/intro)
- [Tailwind CSS documentation](https://tailwindcss.com/docs)
- [Vitest documentation](https://vitest.dev/)
- [Testing Library guiding principles](https://testing-library.com/docs/guiding-principles/)
- [Mock Service Worker documentation](https://mswjs.io/docs/)
- [Playwright documentation](https://playwright.dev/docs/intro)
- [OWASP client-side security risks](https://owasp.org/www-project-top-10-client-side-security-risks/)

When upstream guidance changes, evaluate it deliberately. Do not copy new rules automatically if they conflict with FlowChat's backend contracts, security model, current toolchain, or established local architecture.
