import { queryOptions, useQuery } from '@tanstack/react-query';
import { z } from 'zod';

import { api } from '@/lib/api-client';

const groupConversationSchema = z.object({
  conversationId: z.uuid(),
  name: z.string(),
  participantCount: z.number().int().nonnegative(),
  lastReadMsgSeqNum: z.number().int().nonnegative(),
  currentMsgSeqNum: z.number().int().nonnegative(),
});

const getGroupsResponseSchema = z.object({
  groupConversations: z.array(groupConversationSchema),
});

export type GroupConversation = z.infer<typeof groupConversationSchema>;
export type GetGroupsResponse = z.infer<typeof getGroupsResponseSchema>;

export async function getGroups(): Promise<GetGroupsResponse> {
  const response = await api.get('/api/conversations/group');

  return getGroupsResponseSchema.parse(response.data);
}

export function getGroupsQueryOptions() {
  return queryOptions({
    queryKey: ['group-conversations'],
    queryFn: getGroups,
  });
}

type UseGroupsOptions = {
  queryConfig?: Omit<ReturnType<typeof getGroupsQueryOptions>, 'queryKey' | 'queryFn'>;
};

export function useGroups({ queryConfig }: UseGroupsOptions = {}) {
  return useQuery({
    ...getGroupsQueryOptions(),
    ...queryConfig,
  });
}
