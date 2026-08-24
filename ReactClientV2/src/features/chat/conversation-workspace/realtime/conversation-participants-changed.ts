import { z } from 'zod';

const conversationParticipantsChangedSchema = z.object({
  conversationId: z.guid(),
  conversationType: z.union([z.literal(1), z.literal(2)]),
  participantUserIds: z.array(z.guid()),
});

export const conversationParticipantsAddedSchema = conversationParticipantsChangedSchema;
export const conversationParticipantsRemovedSchema = conversationParticipantsChangedSchema;

export type ConversationParticipantsChangedEvent = z.infer<
  typeof conversationParticipantsChangedSchema
>;

export function parseConversationParticipantsAdded(
  payload: unknown,
): ConversationParticipantsChangedEvent | null {
  const parsed = conversationParticipantsAddedSchema.safeParse(payload);
  return parsed.success ? parsed.data : null;
}

export function parseConversationParticipantsRemoved(
  payload: unknown,
): ConversationParticipantsChangedEvent | null {
  const parsed = conversationParticipantsRemovedSchema.safeParse(payload);
  return parsed.success ? parsed.data : null;
}
