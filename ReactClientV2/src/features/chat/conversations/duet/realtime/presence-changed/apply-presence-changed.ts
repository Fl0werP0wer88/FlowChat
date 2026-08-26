import type { GetDuetsWithPresenceResponse } from '../../api/get-duets-with-presence';

import { parsePresenceChanged } from './presence-changed-event';

export function applyPresenceChanged(
  current: GetDuetsWithPresenceResponse | undefined,
  payload: unknown,
): GetDuetsWithPresenceResponse | undefined {
  if (!current) return current;

  const event = parsePresenceChanged(payload);
  if (!event) return current;

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
