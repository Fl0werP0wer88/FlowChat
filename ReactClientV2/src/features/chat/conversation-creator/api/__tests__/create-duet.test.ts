import { http, HttpResponse } from 'msw';

import { server } from '@/testing/mocks/server';

import { createDuet } from '../create-duet';

const conversationId = '40c3cd3b-69d8-4af3-b1a7-f9174537fb97';
const partnerUserId = '14c11faa-8bd7-4608-abcf-26985f3f62be';

describe('create duet API', () => {
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
});
