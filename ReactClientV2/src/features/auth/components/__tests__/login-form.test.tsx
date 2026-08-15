import { http, HttpResponse } from 'msw';
import { createMemoryRouter, RouterProvider } from 'react-router-dom';

import { server } from '@/testing/mocks/server';
import { renderWithProviders, screen, userEvent, waitFor } from '@/testing/test-utils';
import { createAccessToken } from '@/testing/token-fixture';

import { LoginForm } from '../login-form';

describe('LoginForm', () => {
  it('submits the password grant and redirects to the requested route', async () => {
    let submittedUsername = '';
    server.use(
      http.post('*/api/users/login', async ({ request }) => {
        const payload = await request.formData();
        submittedUsername = String(payload.get('username'));
        return HttpResponse.json({ access_token: createAccessToken(), expires_in: 3_600 });
      }),
    );
    const router = createMemoryRouter(
      [
        { path: '/login', element: <LoginForm /> },
        { path: '/chat', element: <h1>Chat workspace</h1> },
      ],
      { initialEntries: ['/login?redirectTo=%2Fchat'] },
    );
    const user = userEvent.setup();
    renderWithProviders(<RouterProvider router={router} />);

    await user.type(screen.getByLabelText('Email or FriendlyUserId'), 'alex.morgan');
    await user.type(screen.getByLabelText('Password'), 'correct-password');
    await user.click(screen.getByRole('button', { name: 'Sign in' }));

    expect(await screen.findByRole('heading', { name: 'Chat workspace' })).toBeInTheDocument();
    expect(submittedUsername).toBe('alex.morgan');
  });

  it('shows validation and API errors without navigating', async () => {
    const router = createMemoryRouter([{ path: '/login', element: <LoginForm /> }], {
      initialEntries: ['/login'],
    });
    const user = userEvent.setup();
    renderWithProviders(<RouterProvider router={router} />);

    await user.click(screen.getByRole('button', { name: 'Sign in' }));
    expect(await screen.findByText('Enter your email or FriendlyUserId.')).toBeInTheDocument();

    await user.type(screen.getByLabelText('Email or FriendlyUserId'), 'invalid');
    await user.type(screen.getByLabelText('Password'), 'wrong');
    await user.click(screen.getByRole('button', { name: 'Sign in' }));

    await waitFor(() => expect(screen.getByText('Invalid credentials.')).toBeInTheDocument());
    expect(router.state.location.pathname).toBe('/login');
  });
});
