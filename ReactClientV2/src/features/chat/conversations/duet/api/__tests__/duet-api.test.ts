import { http, HttpResponse } from 'msw';

import { server } from '@/testing/mocks/server';

import { createDuet } from '../create-duet';
import { getDuetsWithPresence } from '../get-duets-with-presence';

const conversationId = '40c3cd3b-69d8-4af3-b1a7-f9174537fb97';
const partnerUserId = '14c11faa-8bd7-4608-abcf-26985f3f62be';
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

  it('creates a duet using the backend request contract', async () => {
    let requestPayload: Record<string, unknown> = {};
    server.use(
      http.put('*/api/conversations/duet', async ({ request }) => {
        requestPayload = (await request.json()) as Record<string, unknown>;
        return HttpResponse.json(
          {
            conversationId,
            participants: [
              {
                userId: partnerUserId,
                displayName: 'Alex Morgan',
                avatarUrl: null,
                participantUserId: partnerUserId,
              },
            ],
          },
          { status: 201 },
        );
      }),
    );

    const result = await createDuet({ data: { partnerUserId } });

    expect(requestPayload).toEqual({ partnerUserId });
    expect(result.conversationId).toBe(conversationId);
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
