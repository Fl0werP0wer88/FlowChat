import { api } from '@/lib/api-client';
import { registerResponseSchema, type NormalizedRegisterInput } from '@/types/auth';

export async function registerUser(input: NormalizedRegisterInput): Promise<{ id: string }> {
  const response = await api.put(
    '/api/users',
    {
      id: crypto.randomUUID(),
      email: input.email,
      friendlyUserId: input.friendlyUserId,
      password: input.password,
      firstName: input.firstName,
      lastName: input.lastName,
      organization: input.organization,
    },
    {
      skipAuth: true,
      skipAuthRefresh: true,
    },
  );

  return registerResponseSchema.parse(response.data);
}
