import { api } from '@/lib/api-client';

export async function confirmEmail(token: string): Promise<true> {
  await api.post(
    '/api/userprofiles/email-verification/confirm',
    { token: token.trim() },
    {
      skipAuth: true,
      skipAuthRefresh: true,
    },
  );

  return true;
}
