import cors from 'cors';
import express from 'express';

import { createAccessToken } from './src/testing/token-fixture';

const app = express();
const port = 18080;
const retryAttempts = new Map<string, number>();

app.use(
  cors({
    origin: ['http://127.0.0.1:5173', 'http://127.0.0.1:5174'],
    credentials: true,
  }),
);
app.use(express.json());
app.use(express.urlencoded({ extended: false }));

app.get('/health', (_request, response) => response.status(200).send('ok'));

app.post('/api/users/login', (request, response) => {
  const username = String(request.body.username ?? '');
  const password = String(request.body.password ?? '');

  if (!username || password === 'invalid-password') {
    return response.status(401).json({
      error: 'invalid_grant',
      error_description: 'Invalid credentials or account is not confirmed.',
    });
  }

  response.cookie('rt', 'e2e-refresh-token', {
    httpOnly: true,
    sameSite: 'strict',
    path: '/api/users',
  });

  return response.json({
    access_token: createAccessToken({
      email: username.includes('@') ? username : 'alex@example.com',
      friendlyUserId: username.includes('@') ? 'alex.morgan' : username,
    }),
    expires_in: 3_600,
  });
});

app.post('/api/users/refresh-token', (request, response) => {
  const cookie = request.headers.cookie ?? '';
  if (!cookie.includes('rt=e2e-refresh-token')) {
    return response.status(401).json({
      error: 'invalid_grant',
      error_description: 'Invalid or expired refresh token.',
    });
  }

  return response.json({ access_token: createAccessToken(), expires_in: 3_600 });
});

app.post('/api/users/logout', (_request, response) => {
  response.clearCookie('rt', { sameSite: 'strict', path: '/api/users' });
  return response.status(204).send();
});

app.put('/api/users', (request, response) => {
  if (request.body.email === 'taken@example.com') {
    return response.status(409).json({
      title: 'Conflict',
      detail: 'Account with the provided email already exists.',
    });
  }

  return response.status(201).json({ id: '40c3cd3b-69d8-4af3-b1a7-f9174537fb97' });
});

app.post('/api/userprofiles/email-verification/confirm', (request, response) => {
  const token = String(request.body.token ?? '');
  if (!token || token === 'invalid') {
    return response.status(400).json({
      title: 'Invalid token',
      detail: 'The verification link is invalid or expired.',
    });
  }

  if (token === 'retryable') {
    const attempt = (retryAttempts.get(token) ?? 0) + 1;
    retryAttempts.set(token, attempt);
    if (attempt === 1) {
      return response.status(500).json({ detail: 'Temporary verification failure.' });
    }
  }

  return response.status(204).send();
});

app.listen(port, '127.0.0.1', () => {
  process.stdout.write(`FlowChat mock API listening at http://127.0.0.1:${port}\n`);
});
