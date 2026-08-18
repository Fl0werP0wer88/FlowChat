import { http, HttpResponse } from 'msw';

import { server } from '@/testing/mocks/server';

import { getDuetsWithPresence } from '../get-duets-with-presence';

const conversationId = '40c3cd3b-69d8-4af3-b1a7-f9174537fb97';
const seededPartnerUserId = '00000000-0000-0000-0000-000000000034';

describe('duet conversation API', () => {
  it('gets duet conversations with presence and validates the response', async () => {
    server.use(
      http.get('*/api/aggregate/conversations/duets', () =>
        HttpResponse.json({
          conversations: [
            {
              partnerUserId: seededPartnerUserId,
              displayName: 'Alex Morgan',
              avatarUrl: null,
              email: 'alex@example.com',
              isBlocked: false,
              isBlockedByPartner: false,
              isMuted: false,
              isHidden: false,
              conversationId,
              lastReadMsgSeqNum: 4,
              currentMsgSeqNum: 7,
              unreadCount: 3,
              status: 'Active',
              presenceChangedAtUtc: '2026-08-17T20:00:00+00:00',
            },
          ],
        }),
      ),
    );

    const result = await getDuetsWithPresence();

    expect(result.conversations).toHaveLength(1);
    expect(result.conversations[0]).toMatchObject({
      partnerUserId: seededPartnerUserId,
      status: 'Active',
    });
  });

  it('rejects an invalid success response', async () => {
    server.use(
      http.get('*/api/aggregate/conversations/duets', () =>
        HttpResponse.json({ conversations: [{ conversationId: 'not-a-guid' }] }),
      ),
    );

    await expect(getDuetsWithPresence()).rejects.toBeDefined();
  });
});
