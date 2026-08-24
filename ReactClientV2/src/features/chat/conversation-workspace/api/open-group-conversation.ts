import { queryOptions, useQuery } from '@tanstack/react-query';
import { z } from 'zod';

import { api } from '@/lib/api-client';

import {
  createConversationMessageBuffer,
  type BufferedConversationSnapshot,
} from '../cache/conversation-message-buffer';

import {
  conversationMessageSchema,
  conversationParticipantSchema,
  conversationSequenceNumberSchema,
} from './conversation-contracts';

export const openGroupConversationInputSchema = z.object({
  conversationId: z.guid(),
});

const openGroupConversationResponseSchema = z.object({
  conversationId: z.guid(),
  name: z.string(),
  participants: z.array(conversationParticipantSchema),
  messages: z.array(conversationMessageSchema),
  nextBeforeSequenceNum: conversationSequenceNumberSchema.nullable(),
  currentSequenceNum: conversationSequenceNumberSchema,
  hasMore: z.boolean(),
});

export type OpenGroupConversationInput = z.infer<typeof openGroupConversationInputSchema>;
export type OpenGroupConversationResponse = z.infer<typeof openGroupConversationResponseSchema>;
export type OpenGroupConversationCacheEntry =
  BufferedConversationSnapshot<OpenGroupConversationResponse>;

export async function openGroupConversation(
  input: OpenGroupConversationInput,
  signal?: AbortSignal,
): Promise<OpenGroupConversationResponse> {
  const data = openGroupConversationInputSchema.parse(input);
  const response = await api.put('/api/aggregate/conversations/group/open', data, { signal });

  return openGroupConversationResponseSchema.parse(response.data);
}

export function openGroupConversationQueryOptions(input: OpenGroupConversationInput) {
  const data = openGroupConversationInputSchema.parse(input);

  return queryOptions({
    queryKey: ['conversation-workspace', 'group', data.conversationId],
    queryFn: async ({ signal }) =>
      createConversationMessageBuffer(await openGroupConversation(data, signal)),
  });
}

type UseOpenGroupConversationOptions = {
  input: OpenGroupConversationInput;
  queryConfig?: Omit<ReturnType<typeof openGroupConversationQueryOptions>, 'queryKey' | 'queryFn'>;
};

export function useOpenGroupConversation({ input, queryConfig }: UseOpenGroupConversationOptions) {
  return useQuery({
    ...openGroupConversationQueryOptions(input),
    ...queryConfig,
  });
}
