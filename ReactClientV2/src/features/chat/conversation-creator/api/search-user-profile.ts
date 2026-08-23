import { queryOptions, useQuery } from '@tanstack/react-query';
import { z } from 'zod';

import { api } from '@/lib/api-client';

const optionalSearchCriterion = (maximumLength: number, message: string) =>
  z
    .string()
    .trim()
    .max(maximumLength, message)
    .optional()
    .transform((value) => value || undefined);

export const searchUserProfilesInputSchema = z
  .object({
    firstName: optionalSearchCriterion(100, 'First name cannot be longer than 100 characters.'),
    lastName: optionalSearchCriterion(100, 'Last name cannot be longer than 100 characters.'),
    organization: optionalSearchCriterion(
      200,
      'Organization cannot be longer than 200 characters.',
    ),
  })
  .refine((input) => input.firstName || input.lastName || input.organization, {
    message: 'Enter at least one search criterion.',
  });

const emailSchema = z.object({
  id: z.guid(),
  address: z.string(),
  isMain: z.boolean(),
  isAuth: z.boolean(),
  isConfirmed: z.boolean(),
  isVisible: z.boolean(),
});

const phoneSchema = z.object({
  id: z.guid(),
  number: z.string(),
  isMain: z.boolean(),
  isConfirmed: z.boolean(),
  isVisible: z.boolean(),
});

const userProfileSchema = z.object({
  id: z.guid(),
  friendlyUserId: z.string(),
  firstName: z.string().nullable(),
  lastName: z.string().nullable(),
  organization: z.string().nullable(),
  avatarUrl: z.string().nullable(),
  bio: z.string().nullable(),
  isActive: z.boolean(),
  lastSeenAtUtc: z.iso.datetime({ offset: true }).nullable(),
  emails: z.array(emailSchema),
  phones: z.array(phoneSchema),
});

const searchUserProfilesResponseSchema = z.object({
  userProfiles: z.array(userProfileSchema),
});

export type SearchUserProfilesInput = z.input<typeof searchUserProfilesInputSchema>;
export type UserProfile = z.infer<typeof userProfileSchema>;
export type SearchUserProfilesResponse = z.infer<typeof searchUserProfilesResponseSchema>;

export async function searchUserProfiles(
  input: SearchUserProfilesInput,
  signal?: AbortSignal,
): Promise<SearchUserProfilesResponse> {
  const params = searchUserProfilesInputSchema.parse(input);
  const response = await api.get('/api/userprofiles/search', { params, signal });

  return searchUserProfilesResponseSchema.parse(response.data);
}

export function searchUserProfilesQueryOptions(input: SearchUserProfilesInput) {
  const criteria = searchUserProfilesInputSchema.parse(input);

  return queryOptions({
    queryKey: ['user-profiles', 'search', criteria],
    queryFn: ({ signal }) => searchUserProfiles(criteria, signal),
  });
}

type UseSearchUserProfilesOptions = {
  input: SearchUserProfilesInput;
  queryConfig?: Omit<ReturnType<typeof searchUserProfilesQueryOptions>, 'queryKey' | 'queryFn'>;
};

export function useSearchUserProfiles({ input, queryConfig }: UseSearchUserProfilesOptions) {
  return useQuery({
    ...searchUserProfilesQueryOptions(input),
    ...queryConfig,
  });
}
