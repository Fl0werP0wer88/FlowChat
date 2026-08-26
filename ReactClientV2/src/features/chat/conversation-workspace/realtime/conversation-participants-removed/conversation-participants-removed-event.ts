import { z } from 'zod';

export const conversationParticipantsRemovedSchema = z.object({
  conversationId: z.guid(),
  conversationType: z.union([z.literal(1), z.literal(2)]),
  participantUserIds: z.array(z.guid()),
});

export type ConversationParticipantsRemovedEvent = z.infer<
  typeof conversationParticipantsRemovedSchema
>;

export function parseConversationParticipantsRemoved(
  payload: unknown,
): ConversationParticipantsRemovedEvent | null {
  const parsed = conversationParticipantsRemovedSchema.safeParse(payload);
  return parsed.success ? parsed.data : null;
}
