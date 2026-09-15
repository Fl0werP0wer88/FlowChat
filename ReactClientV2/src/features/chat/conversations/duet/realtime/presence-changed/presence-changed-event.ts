import { z } from 'zod';

import { presenceStatusSchema } from '../../api/get-duets-with-presence';

export const presenceChangedSchema = z.object({
  userId: z.uuid(),
  status: presenceStatusSchema,
  changedAtUtc: z.iso.datetime({ offset: true }),
});

export type PresenceChangedEvent = z.infer<typeof presenceChangedSchema>;

export function parsePresenceChanged(payload: unknown): PresenceChangedEvent | null {
  const parsed = presenceChangedSchema.safeParse(payload);
  return parsed.success ? parsed.data : null;
}
