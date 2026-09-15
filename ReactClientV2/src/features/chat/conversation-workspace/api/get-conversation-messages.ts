import { z } from 'zod';

import { api } from '@/lib/api-client';

import {
  conversationMessageSchema,
  conversationSequenceNumberSchema,
} from './conversation-contracts';

export const getConversationMessagesInputSchema = z.object({
  conversationId: z.guid(),
  beforeSequenceNum: conversationSequenceNumberSchema
    .nullish()
    .transform((value) => value ?? undefined),
  limit: z.number().int().min(1).max(100).default(10),
});

const getConversationMessagesResponseSchema = z.object({
  items: z.array(conversationMessageSchema),
  nextBeforeSequenceNum: conversationSequenceNumberSchema.nullable(),
  currentSequenceNum: conversationSequenceNumberSchema,
  hasMore: z.boolean(),
});

export type GetConversationMessagesInput = z.input<typeof getConversationMessagesInputSchema>;
export type GetConversationMessagesResponse = z.infer<typeof getConversationMessagesResponseSchema>;

export async function getConversationMessages(
  input: GetConversationMessagesInput,
  signal?: AbortSignal,
): Promise<GetConversationMessagesResponse> {
  const { conversationId, ...params } = getConversationMessagesInputSchema.parse(input);
  const response = await api.get(`/api/chat/conversations/${conversationId}/messages`, {
    params,
    signal,
  });

  return getConversationMessagesResponseSchema.parse(response.data);
}
