import { z } from 'zod';

import {
  conversationSequenceNumberSchema,
  type ConversationMessage,
} from '../../api/conversation-contracts';

export const messageReceivedSchema = z.object({
  messageId: z.guid(),
  conversationId: z.guid(),
  senderUserId: z.guid(),
  text: z.string(),
  sequenceNum: conversationSequenceNumberSchema,
  sentAtUtc: z.iso.datetime({ offset: true }),
  deliveredAtUtc: z.iso.datetime({ offset: true }),
});

export type MessageReceivedEvent = z.infer<typeof messageReceivedSchema>;

export function parseMessageReceived(payload: unknown): ConversationMessage | null {
  const parsed = messageReceivedSchema.safeParse(payload);
  if (!parsed.success) return null;

  const event = parsed.data;
  return {
    id: event.messageId,
    conversationId: event.conversationId,
    senderUserId: event.senderUserId,
    text: event.text,
    sentAtUtc: event.sentAtUtc,
    sequenceNum: event.sequenceNum,
  };
}
