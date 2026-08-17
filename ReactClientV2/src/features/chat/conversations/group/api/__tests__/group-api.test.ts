import { http, HttpResponse } from 'msw';

import { server } from '@/testing/mocks/server';

import { createGroup } from '../create-group';
import { getGroups } from '../get-groups';

const conversationId = '40c3cd3b-69d8-4af3-b1a7-f9174537fb97';
const participantUserId = '14c11faa-8bd7-4608-abcf-26985f3f62be';

describe('group conversation API', () => {
  it('gets group conversations and validates the response', async () => {
    server.use(
      http.get('*/api/conversations/group', () =>
        HttpResponse.json({
          groupConversations: [
            {
              conversationId,
              name: 'Product team',
              participantCount: 3,
              lastReadMsgSeqNum: 4,
              currentMsgSeqNum: 7,
            },
          ],
        }),
      ),
    );

    await expect(getGroups()).resolves.toEqual({
      groupConversations: [
        {
          conversationId,
          name: 'Product team',
          participantCount: 3,
          lastReadMsgSeqNum: 4,
          currentMsgSeqNum: 7,
        },
      ],
    });
  });

  it('creates a group with a generated conversation ID and normalized name', async () => {
    let requestPayload: Record<string, unknown> = {};
    server.use(
      http.post('*/api/conversations/group', async ({ request }) => {
        requestPayload = (await request.json()) as Record<string, unknown>;
        return HttpResponse.json(
          {
            conversationId,
            name: 'Product team',
            participants: [
              {
                userId: participantUserId,
                displayName: 'Alex Morgan',
                avatarUrl: null,
                participantUserId,
              },
            ],
          },
          { status: 201 },
        );
      }),
    );

    const result = await createGroup({
      data: {
        participantUserIds: [participantUserId],
        name: '  Product team  ',
      },
    });

    expect(requestPayload).toEqual({
      conversationId: expect.any(String),
      participantUserIds: [participantUserId],
      name: 'Product team',
    });
    expect(result.conversationId).toBe(conversationId);
  });
});
