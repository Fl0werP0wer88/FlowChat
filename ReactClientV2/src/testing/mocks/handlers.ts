import { http, HttpResponse } from 'msw';

import { createAccessToken } from '@/testing/token-fixture';

export const handlers = [
  http.post('*/api/users/refresh-token', () =>
    HttpResponse.json(
      { error: 'invalid_grant', error_description: 'Invalid or expired refresh token.' },
      { status: 401 },
    ),
  ),
  http.post('*/api/users/login', async ({ request }) => {
    const form = await request.formData();
    const username = String(form.get('username') ?? '');
    if (username === 'invalid') {
      return HttpResponse.json(
        { error: 'invalid_grant', error_description: 'Invalid credentials.' },
        { status: 401 },
      );
    }

    return HttpResponse.json({
      access_token: createAccessToken({
        email: username.includes('@') ? username : 'alex@example.com',
        friendlyUserId: username.includes('@') ? 'alex.morgan' : username,
      }),
      expires_in: 3_600,
    });
  }),
  http.put('*/api/users', async ({ request }) => {
    const payload = (await request.json()) as { email?: string };
    if (payload.email === 'taken@example.com') {
      return HttpResponse.json(
        { title: 'Conflict', detail: 'Account with the provided email already exists.' },
        { status: 409 },
      );
    }

    return HttpResponse.json({ id: '40c3cd3b-69d8-4af3-b1a7-f9174537fb97' }, { status: 201 });
  }),
  http.post('*/api/users/logout', () => new HttpResponse(null, { status: 204 })),
  http.post('*/api/userprofiles/email-verification/confirm', async ({ request }) => {
    const payload = (await request.json()) as { token?: string };
    if (payload.token === 'invalid') {
      return HttpResponse.json(
        { title: 'Invalid token', detail: 'The verification link is invalid or expired.' },
        { status: 400 },
      );
    }

    return new HttpResponse(null, { status: 204 });
  }),
];
