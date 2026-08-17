import { queryOptions, useQuery } from '@tanstack/react-query';
import { z } from 'zod';

import { api } from '@/lib/api-client';

export const presenceStatusSchema = z.enum(['Active', 'AFK', 'Busy', 'Invisible']);

const duetConversationWithPresenceSchema = z.object({
  partnerUserId: z.uuid(),
  displayName: z.string().nullable(),
  avatarUrl: z.string().nullable(),
  email: z.string().nullable(),
  isBlocked: z.boolean(),
  isBlockedByPartner: z.boolean(),
  isMuted: z.boolean(),
  isHidden: z.boolean(),
  conversationId: z.uuid(),
  lastReadMsgSeqNum: z.number().int().nonnegative(),
  currentMsgSeqNum: z.number().int().nonnegative(),
  unreadCount: z.number().int().nonnegative(),
  status: presenceStatusSchema,
  presenceChangedAtUtc: z.iso.datetime({ offset: true }),
});

const getDuetsWithPresenceResponseSchema = z.object({
  conversations: z.array(duetConversationWithPresenceSchema),
});

export type PresenceStatus = z.infer<typeof presenceStatusSchema>;
export type DuetConversationWithPresence = z.infer<typeof duetConversationWithPresenceSchema>;
export type GetDuetsWithPresenceResponse = z.infer<typeof getDuetsWithPresenceResponseSchema>;

export async function getDuetsWithPresence(): Promise<GetDuetsWithPresenceResponse> {
  const response = await api.get('/api/aggregate/conversations/duets');

  return getDuetsWithPresenceResponseSchema.parse(response.data);
}

export function getDuetsWithPresenceQueryOptions() {
  return queryOptions({
    queryKey: ['duet-conversations-with-presence'],
    queryFn: getDuetsWithPresence,
  });
}

type UseDuetsWithPresenceOptions = {
  queryConfig?: Omit<ReturnType<typeof getDuetsWithPresenceQueryOptions>, 'queryKey' | 'queryFn'>;
};

export function useDuetsWithPresence({ queryConfig }: UseDuetsWithPresenceOptions = {}) {
  return useQuery({
    ...getDuetsWithPresenceQueryOptions(),
    ...queryConfig,
  });
}
