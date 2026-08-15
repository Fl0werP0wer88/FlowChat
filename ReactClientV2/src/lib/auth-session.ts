import { z } from 'zod';

import { api, configureAuthRefresh } from '@/lib/api-client';
import { queryClient } from '@/lib/query-client';
import { getAuthState } from '@/stores/auth-store';
import {
  authTokenResponseSchema,
  type AuthSession,
  type AuthTokenResponse,
  type AuthUser,
} from '@/types/auth';

const jwtPayloadSchema = z
  .object({
    sub: z.string().min(1),
    email: z.string().optional(),
    preferred_username: z.string().optional(),
    unique_name: z.string().optional(),
    exp: z.number().int().positive().optional(),
    role: z.union([z.string(), z.array(z.string())]).optional(),
    'http://schemas.microsoft.com/ws/2008/06/identity/claims/role': z
      .union([z.string(), z.array(z.string())])
      .optional(),
  })
  .passthrough();

let refreshPromise: Promise<AuthSession | null> | null = null;
let bootstrapPromise: Promise<void> | null = null;

function decodeBase64Url(value: string): string {
  const base64 = value.replace(/-/g, '+').replace(/_/g, '/');
  const padded = base64.padEnd(base64.length + ((4 - (base64.length % 4)) % 4), '=');
  const bytes = Uint8Array.from(atob(padded), (character) => character.charCodeAt(0));
  return new TextDecoder().decode(bytes);
}

function parseToken(accessToken: string) {
  const payload = accessToken.split('.')[1];
  if (!payload) throw new Error('The access token is malformed.');

  try {
    return jwtPayloadSchema.parse(JSON.parse(decodeBase64Url(payload)));
  } catch {
    throw new Error('The access token payload is invalid.');
  }
}

function normalizeRoles(value: string | string[] | undefined): string[] {
  if (!value) return [];
  return Array.isArray(value) ? value : [value];
}

function resolveExpiration(response: AuthTokenResponse, tokenExpiration?: number): string {
  if (response.expiresAtUtc) return response.expiresAtUtc;
  if (response.expires_in) return new Date(Date.now() + response.expires_in * 1_000).toISOString();
  if (tokenExpiration) return new Date(tokenExpiration * 1_000).toISOString();
  throw new Error('Authentication response does not include token expiration.');
}

export function establishSession(payload: unknown): AuthSession {
  const response = authTokenResponseSchema.parse(payload);
  const accessToken = response.access_token ?? response.accessToken;

  if (!accessToken) throw new Error('Authentication response does not contain an access token.');

  const claims = parseToken(accessToken);
  const roleClaim =
    claims.role ?? claims['http://schemas.microsoft.com/ws/2008/06/identity/claims/role'];
  const user: AuthUser = {
    id: claims.sub,
    email: claims.email ?? null,
    friendlyUserId: claims.preferred_username ?? claims.unique_name ?? claims.email ?? claims.sub,
    roles: normalizeRoles(roleClaim),
  };
  const session: AuthSession = {
    accessToken,
    expiresAtUtc: resolveExpiration(response, claims.exp),
    user,
  };

  getAuthState().setSession(session);
  return session;
}

export function refreshSession(): Promise<AuthSession | null> {
  if (refreshPromise) return refreshPromise;

  refreshPromise = api
    .post('/api/users/refresh-token', new URLSearchParams({ grant_type: 'refresh_token' }), {
      headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
      skipAuth: true,
      skipAuthRefresh: true,
    })
    .then((response) => establishSession(response.data))
    .catch(() => {
      getAuthState().clearSession();
      return null;
    })
    .finally(() => {
      refreshPromise = null;
    });

  return refreshPromise;
}

export function bootstrapSession(): Promise<void> {
  const state = getAuthState();
  if (state.bootstrapStatus === 'ready') return Promise.resolve();
  if (bootstrapPromise) return bootstrapPromise;

  state.setBootstrapStatus('loading');
  bootstrapPromise = refreshSession()
    .then(() => undefined)
    .finally(() => {
      getAuthState().setBootstrapStatus('ready');
      bootstrapPromise = null;
    });

  return bootstrapPromise;
}

export async function logoutSession(): Promise<void> {
  try {
    await api.post('/api/users/logout', undefined, {
      skipAuth: true,
      skipAuthRefresh: true,
    });
  } finally {
    getAuthState().clearSession();
    queryClient.clear();
  }
}

configureAuthRefresh(async () => (await refreshSession())?.accessToken ?? null);
