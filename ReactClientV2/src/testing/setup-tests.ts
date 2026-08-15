import '@testing-library/jest-dom/vitest';

import { cleanup } from '@testing-library/react';

import { queryClient } from '@/lib/query-client';
import { useAuthStore } from '@/stores/auth-store';
import { server } from '@/testing/mocks/server';

beforeAll(() => server.listen({ onUnhandledRequest: 'error' }));

beforeEach(() => {
  useAuthStore.setState({ session: null, bootstrapStatus: 'ready' });
  queryClient.clear();
});

afterEach(() => {
  cleanup();
  server.resetHandlers();
});

afterAll(() => server.close());
