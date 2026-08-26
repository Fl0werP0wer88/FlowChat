import { z } from 'zod';

export const groupConversationChangedSchema = z.object({
  conversationId: z.guid(),
  type: z.literal(2),
  name: z.string().nullable(),
});

export type GroupConversationChangedEvent = z.infer<typeof groupConversationChangedSchema>;

export function parseGroupConversationChanged(
  payload: unknown,
): GroupConversationChangedEvent | null {
  const parsed = groupConversationChangedSchema.safeParse(payload);
  return parsed.success ? parsed.data : null;
}
