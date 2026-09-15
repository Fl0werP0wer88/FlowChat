import { infiniteQueryOptions, useInfiniteQuery } from '@tanstack/react-query';
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

const userProfileSchema = z.object({
  id: z.guid(),
  friendlyUserId: z.string(),
  firstName: z.string().nullable(),
  lastName: z.string().nullable(),
  organization: z.string().nullable(),
  avatarUrl: z.string().nullable(),
});

const searchUserProfilesResponseSchema = z.object({
  items: z.array(userProfileSchema),
  nextCursor: z.string().nullable(),
  hasMore: z.boolean(),
});

const searchUserProfilesPageParamsSchema = z.object({
  cursor: z.string().nullable(),
  limit: z.literal(20),
});

const searchUserProfilesPageSize = 20;

export type SearchUserProfilesInput = z.input<typeof searchUserProfilesInputSchema>;
export type UserProfile = z.infer<typeof userProfileSchema>;
export type SearchUserProfilesResponse = z.infer<typeof searchUserProfilesResponseSchema>;

export async function searchUserProfiles(
  input: SearchUserProfilesInput,
  cursor: string | null = null,
  signal?: AbortSignal,
): Promise<SearchUserProfilesResponse> {
  const criteria = searchUserProfilesInputSchema.parse(input);
  const pageParams = searchUserProfilesPageParamsSchema.parse({
    cursor,
    limit: searchUserProfilesPageSize,
  });
  const params = {
    ...criteria,
    ...(pageParams.cursor ? { cursor: pageParams.cursor } : {}),
    limit: pageParams.limit,
  };
  const response = await api.get('/api/userprofiles/search/range/ascending', { params, signal });

  return searchUserProfilesResponseSchema.parse(response.data);
}

export function searchUserProfilesQueryOptions(input: SearchUserProfilesInput) {
  const criteria = searchUserProfilesInputSchema.parse(input);

  return infiniteQueryOptions({
    queryKey: [
      'user-profiles',
      'search',
      'range',
      'ascending',
      criteria,
      searchUserProfilesPageSize,
    ],
    queryFn: ({ pageParam, signal }) => searchUserProfiles(criteria, pageParam, signal),
    initialPageParam: null as string | null,
    getNextPageParam: (lastPage) =>
      lastPage.hasMore && lastPage.nextCursor ? lastPage.nextCursor : undefined,
  });
}

type UseSearchUserProfilesOptions = {
  input: SearchUserProfilesInput;
  queryConfig?: Omit<ReturnType<typeof searchUserProfilesQueryOptions>, 'queryKey' | 'queryFn'>;
};

export function useSearchUserProfiles({ input, queryConfig }: UseSearchUserProfilesOptions) {
  return useInfiniteQuery({
    ...searchUserProfilesQueryOptions(input),
    ...queryConfig,
  });
}
