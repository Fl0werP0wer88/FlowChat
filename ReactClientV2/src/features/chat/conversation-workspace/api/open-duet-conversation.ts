import { queryOptions, useQuery } from '@tanstack/react-query';
import { z } from 'zod';

import { api } from '@/lib/api-client';

import {
  conversationMessageSchema,
  conversationParticipantSchema,
  conversationSequenceNumberSchema,
} from './conversation-contracts';

export const openDuetConversationInputSchema = z.object({
  partnerUserId: z.guid(),
  knownConversationId: z.guid().nullable().default(null),
});

const openDuetConversationResponseSchema = z.object({
  conversationId: z.guid(),
  participants: z.array(conversationParticipantSchema),
  messages: z.array(conversationMessageSchema),
  nextBeforeSequenceNum: conversationSequenceNumberSchema.nullable(),
  currentSequenceNum: conversationSequenceNumberSchema,
  hasMore: z.boolean(),
});

export type OpenDuetConversationInput = z.input<typeof openDuetConversationInputSchema>;
export type OpenDuetConversationResponse = z.infer<typeof openDuetConversationResponseSchema>;

export async function openDuetConversation(
  input: OpenDuetConversationInput,
  signal?: AbortSignal,
): Promise<OpenDuetConversationResponse> {
  const data = openDuetConversationInputSchema.parse(input);
  const response = await api.put('/api/aggregate/conversations/duet/open', data, { signal });

  return openDuetConversationResponseSchema.parse(response.data);
}

export function openDuetConversationQueryOptions(input: OpenDuetConversationInput) {
  const data = openDuetConversationInputSchema.parse(input);

  return queryOptions({
    queryKey: ['conversation-workspace', 'duet', data.partnerUserId, data.knownConversationId],
    queryFn: ({ signal }) => openDuetConversation(data, signal),
  });
}

type UseOpenDuetConversationOptions = {
  input: OpenDuetConversationInput;
  queryConfig?: Omit<ReturnType<typeof openDuetConversationQueryOptions>, 'queryKey' | 'queryFn'>;
};

export function useOpenDuetConversation({ input, queryConfig }: UseOpenDuetConversationOptions) {
  return useQuery({
    ...openDuetConversationQueryOptions(input),
    ...queryConfig,
  });
}
