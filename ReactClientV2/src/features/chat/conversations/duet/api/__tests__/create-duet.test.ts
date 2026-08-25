import { http, HttpResponse } from 'msw';

import { server } from '@/testing/mocks/server';

import { createDuet } from '../create-duet';

const conversationId = '00000000-0000-0000-0000-000000000033';
const partnerUserId = '00000000-0000-0000-0000-000000000034';

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
