import { z } from 'zod';

export const conversationSequenceNumberSchema = z.number().int().nonnegative();

export const conversationParticipantSchema = z.object({
  userId: z.guid(),
  displayName: z.string().nullable(),
  avatarUrl: z.string().nullable(),
  participantUserId: z.guid(),
});

export const conversationMessageSchema = z.object({
  id: z.guid(),
  conversationId: z.guid(),
  senderUserId: z.guid(),
  text: z.string(),
  sentAtUtc: z.iso.datetime({ offset: true }),
  sequenceNum: conversationSequenceNumberSchema,
});

export type ConversationParticipant = z.infer<typeof conversationParticipantSchema>;
export type ConversationMessage = z.infer<typeof conversationMessageSchema>;
