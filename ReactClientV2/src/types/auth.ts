import { z } from 'zod';

export interface AuthUser {
  id: string;
  email: string | null;
  friendlyUserId: string;
  roles: string[];
}

export interface AuthSession {
  accessToken: string;
  expiresAtUtc: string;
  user: AuthUser;
}

export const authTokenResponseSchema = z
  .object({
    access_token: z.string().min(1).optional(),
    accessToken: z.string().min(1).optional(),
    expires_in: z.number().finite().positive().optional(),
    expiresAtUtc: z.iso.datetime({ offset: true }).nullable().optional(),
    token_type: z.string().optional(),
    scope: z.string().optional(),
  })
  .refine((value) => Boolean(value.access_token ?? value.accessToken), {
    message: 'Authentication response does not contain an access token.',
  });

export type AuthTokenResponse = z.infer<typeof authTokenResponseSchema>;

export const loginInputSchema = z.object({
  login: z.string().trim().min(1, 'Enter your email or FriendlyUserId.'),
  password: z.string().min(1, 'Enter your password.'),
});

export type LoginInput = z.infer<typeof loginInputSchema>;

const friendlyUserIdPattern = /^[a-z0-9](?:[a-z0-9.-]*[a-z0-9])?$/;
const optionalProfileField = z
  .string()
  .trim()
  .transform((value) => value || undefined);

export const registerInputSchema = z.object({
  email: z.string().trim().pipe(z.email('Enter a valid email address.')),
  friendlyUserId: z
    .string()
    .trim()
    .toLowerCase()
    .min(1, 'Enter a FriendlyUserId.')
    .max(100, 'FriendlyUserId cannot exceed 100 characters.')
    .regex(
      friendlyUserIdPattern,
      "Use lowercase letters, numbers, '-' or '.', and start and end with a letter or number.",
    ),
  password: z.string().min(8, 'Use at least 8 characters.'),
  firstName: optionalProfileField,
  lastName: optionalProfileField,
  organization: optionalProfileField,
});

export type RegisterInput = z.input<typeof registerInputSchema>;
export type NormalizedRegisterInput = z.output<typeof registerInputSchema>;

export const registerResponseSchema = z.object({
  id: z.uuid(),
});

export interface LoginNavigationState {
  prefillLogin?: string;
}
