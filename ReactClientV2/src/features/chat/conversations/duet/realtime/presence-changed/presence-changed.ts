import { z } from 'zod';

import {
  presenceStatusSchema,
  type GetDuetsWithPresenceResponse,
} from '../../api/get-duets-with-presence';

export const presenceChangedSchema = z.object({
  userId: z.uuid(),
  status: presenceStatusSchema,
  changedAtUtc: z.iso.datetime({ offset: true }),
});

export type PresenceChangedEvent = z.infer<typeof presenceChangedSchema>;

export function applyPresenceChanged(
  current: GetDuetsWithPresenceResponse | undefined,
  payload: unknown,
): GetDuetsWithPresenceResponse | undefined {
  if (!current) return current;

  const parsed = presenceChangedSchema.safeParse(payload);
  if (!parsed.success) return current;

  const event = parsed.data;
  let changed = false;
  const conversations = current.conversations.map((conversation) => {
    if (
      conversation.partnerUserId !== event.userId ||
      Date.parse(conversation.presenceChangedAtUtc) >= Date.parse(event.changedAtUtc)
    ) {
      return conversation;
    }

    changed = true;
    return {
      ...conversation,
      status: event.status,
      presenceChangedAtUtc: event.changedAtUtc,
    };
  });

  return changed ? { ...current, conversations } : current;
}
