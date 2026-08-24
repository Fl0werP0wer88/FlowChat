import { useMutation, type UseMutationOptions } from '@tanstack/react-query';
import { z } from 'zod';

import { api } from '@/lib/api-client';

import { conversationSequenceNumberSchema } from './conversation-contracts';

export const sendChatMessageInputSchema = z.object({
  id: z.guid(),
  conversationId: z.guid(),
  text: z.string().trim().min(1, 'Enter a message.'),
});

const sendChatMessageResponseSchema = z.object({
  messageId: z.guid(),
  sentAtUtc: z.iso.datetime({ offset: true }),
  sequenceNum: conversationSequenceNumberSchema,
});

export type SendChatMessageInput = z.infer<typeof sendChatMessageInputSchema>;
export type SendChatMessageResponse = z.infer<typeof sendChatMessageResponseSchema>;

type SendChatMessageVariables = {
  data: SendChatMessageInput;
};

export async function sendChatMessage({
  data,
}: SendChatMessageVariables): Promise<SendChatMessageResponse> {
  const input = sendChatMessageInputSchema.parse(data);
  const response = await api.put('/api/chat/messages', input);

  return sendChatMessageResponseSchema.parse(response.data);
}

type UseSendChatMessageOptions = {
  mutationConfig?: Omit<
    UseMutationOptions<SendChatMessageResponse, Error, SendChatMessageVariables>,
    'mutationFn'
  >;
};

export function useSendChatMessage({ mutationConfig }: UseSendChatMessageOptions = {}) {
  return useMutation({
    ...mutationConfig,
    mutationFn: sendChatMessage,
  });
}
