import { api } from '@/lib/api-client';
import { establishSession } from '@/lib/auth-session';
import type { AuthSession, LoginInput } from '@/types/auth';

export async function loginUser(input: LoginInput): Promise<AuthSession> {
  const payload = new URLSearchParams({
    grant_type: 'password',
    username: input.login.trim(),
    password: input.password,
  });
  const response = await api.post('/api/users/login', payload, {
    headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
    skipAuth: true,
    skipAuthRefresh: true,
  });

  return establishSession(response.data);
}
