import { createMemoryRouter, RouterProvider } from 'react-router-dom';

import { useAuthStore } from '@/stores/auth-store';
import { renderWithProviders, screen, waitFor } from '@/testing/test-utils';
import { createAccessToken } from '@/testing/token-fixture';

import { GuestRoute, ProtectedRoute } from '../route-guards';

const session = {
  accessToken: createAccessToken(),
  expiresAtUtc: new Date(Date.now() + 3_600_000).toISOString(),
  user: {
    id: '82b0c1ca-d57a-43b0-a871-39a97056af89',
    email: 'alex@example.com',
    friendlyUserId: 'alex.morgan',
    roles: ['User'],
  },
};

describe('route guards', () => {
  it('redirects an anonymous chat request to login with redirectTo', async () => {
    const router = createMemoryRouter(
      [
        { element: <ProtectedRoute />, children: [{ path: '/chat', element: <h1>Chat</h1> }] },
        { path: '/login', element: <h1>Login</h1> },
      ],
      { initialEntries: ['/chat'] },
    );

    renderWithProviders(<RouterProvider router={router} />);

    expect(await screen.findByRole('heading', { name: 'Login' })).toBeInTheDocument();
    expect(router.state.location.search).toBe('?redirectTo=%2Fchat');
  });

  it('keeps authenticated users out of guest routes', async () => {
    useAuthStore.getState().setSession(session);
    const router = createMemoryRouter(
      [
        { element: <GuestRoute />, children: [{ path: '/login', element: <h1>Login</h1> }] },
        { path: '/chat', element: <h1>Chat</h1> },
      ],
      { initialEntries: ['/login'] },
    );

    renderWithProviders(<RouterProvider router={router} />);

    await waitFor(() => expect(router.state.location.pathname).toBe('/chat'));
    expect(screen.getByRole('heading', { name: 'Chat' })).toBeInTheDocument();
  });
});
