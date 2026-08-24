import { useMutation, type UseMutationOptions } from '@tanstack/react-query';
import { z } from 'zod';

import { api } from '@/lib/api-client';

import { conversationSequenceNumberSchema } from './conversation-contracts';

export const markConversationAsReadInputSchema = z.object({
  conversationId: z.guid(),
  sequenceNum: conversationSequenceNumberSchema,
});

export type MarkConversationAsReadInput = z.infer<typeof markConversationAsReadInputSchema>;

type MarkConversationAsReadVariables = {
  data: MarkConversationAsReadInput;
};

export async function markConversationAsRead({
  data,
}: MarkConversationAsReadVariables): Promise<void> {
  const { conversationId, sequenceNum } = markConversationAsReadInputSchema.parse(data);

  await api.put(`/api/conversations/${conversationId}/read-state`, { sequenceNum });
}

type UseMarkConversationAsReadOptions = {
  mutationConfig?: Omit<
    UseMutationOptions<void, Error, MarkConversationAsReadVariables>,
    'mutationFn'
  >;
};

export function useMarkConversationAsRead({
  mutationConfig,
}: UseMarkConversationAsReadOptions = {}) {
  return useMutation({
    ...mutationConfig,
    mutationFn: markConversationAsRead,
  });
}
