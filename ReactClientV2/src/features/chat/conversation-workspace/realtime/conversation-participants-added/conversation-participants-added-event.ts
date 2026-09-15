import { z } from 'zod';

export const conversationParticipantsAddedSchema = z.object({
  conversationId: z.guid(),
  conversationType: z.union([z.literal(1), z.literal(2)]),
  participantUserIds: z.array(z.guid()),
});

export type ConversationParticipantsAddedEvent = z.infer<
  typeof conversationParticipantsAddedSchema
>;

export function parseConversationParticipantsAdded(
  payload: unknown,
): ConversationParticipantsAddedEvent | null {
  const parsed = conversationParticipantsAddedSchema.safeParse(payload);
  return parsed.success ? parsed.data : null;
}
