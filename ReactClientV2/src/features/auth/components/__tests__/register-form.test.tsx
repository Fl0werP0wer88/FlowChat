import { http, HttpResponse } from 'msw';
import { createMemoryRouter, RouterProvider } from 'react-router-dom';

import { server } from '@/testing/mocks/server';
import { renderWithProviders, screen, userEvent, waitFor } from '@/testing/test-utils';

import { LoginForm } from '../login-form';
import { RegisterForm } from '../register-form';

describe('RegisterForm', () => {
  it('normalizes the request and prefills login after registration', async () => {
    let requestPayload: Record<string, unknown> = {};
    server.use(
      http.put('*/api/users', async ({ request }) => {
        requestPayload = (await request.json()) as Record<string, unknown>;
        return HttpResponse.json({ id: '40c3cd3b-69d8-4af3-b1a7-f9174537fb97' }, { status: 201 });
      }),
    );
    const router = createMemoryRouter(
      [
        { path: '/register', element: <RegisterForm /> },
        { path: '/login', element: <LoginForm /> },
      ],
      { initialEntries: ['/register'] },
    );
    const user = userEvent.setup();
    renderWithProviders(<RouterProvider router={router} />);

    await user.type(screen.getByLabelText('Email'), 'alex@example.com');
    await user.type(screen.getByLabelText('FriendlyUserId'), 'Alex.Morgan');
    await user.type(screen.getByLabelText('Password'), 'password123');
    await user.click(screen.getByRole('button', { name: 'Create account' }));

    await waitFor(() => expect(router.state.location.pathname).toBe('/login'));
    expect(await screen.findByLabelText('Email or FriendlyUserId')).toHaveValue('alex@example.com');
    expect(requestPayload.friendlyUserId).toBe('alex.morgan');
    expect(requestPayload).not.toHaveProperty('firstName');
    expect(requestPayload.id).toEqual(expect.any(String));
  });

  it('validates email, FriendlyUserId, and password before submitting', async () => {
    const router = createMemoryRouter([{ path: '/register', element: <RegisterForm /> }], {
      initialEntries: ['/register'],
    });
    const user = userEvent.setup();
    renderWithProviders(<RouterProvider router={router} />);

    await user.type(screen.getByLabelText('Email'), 'not-an-email');
    await user.type(screen.getByLabelText('FriendlyUserId'), '.invalid.');
    await user.type(screen.getByLabelText('Password'), 'short');
    await user.click(screen.getByRole('button', { name: 'Create account' }));

    expect(await screen.findByText('Enter a valid email address.')).toBeInTheDocument();
    expect(screen.getByText(/Use lowercase letters/)).toBeInTheDocument();
    expect(screen.getByText('Use at least 8 characters.')).toBeInTheDocument();
  });
});
