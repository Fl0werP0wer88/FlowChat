import { z } from 'zod';

import { api } from '@/lib/api-client';

import {
  conversationMessageSchema,
  conversationSequenceNumberSchema,
} from './conversation-contracts';

export const catchUpConversationMessagesInputSchema = z.object({
  conversationId: z.guid(),
  afterSequenceNum: conversationSequenceNumberSchema,
  throughSequenceNum: conversationSequenceNumberSchema
    .nullish()
    .transform((value) => value ?? undefined),
  limit: z.number().int().min(1).max(100).default(100),
});

const catchUpConversationMessagesResponseSchema = z.object({
  items: z.array(conversationMessageSchema),
  nextAfterSequenceNum: conversationSequenceNumberSchema.nullable(),
  currentSequenceNum: conversationSequenceNumberSchema,
  throughSequenceNum: conversationSequenceNumberSchema,
  hasMore: z.boolean(),
});

export type CatchUpConversationMessagesInput = z.input<
  typeof catchUpConversationMessagesInputSchema
>;
export type CatchUpConversationMessagesResponse = z.infer<
  typeof catchUpConversationMessagesResponseSchema
>;

export async function catchUpConversationMessages(
  input: CatchUpConversationMessagesInput,
  signal?: AbortSignal,
): Promise<CatchUpConversationMessagesResponse> {
  const { conversationId, ...params } = catchUpConversationMessagesInputSchema.parse(input);
  const response = await api.get(`/api/chat/conversations/${conversationId}/messages/catch-up`, {
    params,
    signal,
  });

  return catchUpConversationMessagesResponseSchema.parse(response.data);
}
