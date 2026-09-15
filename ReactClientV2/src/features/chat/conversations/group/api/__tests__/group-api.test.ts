import { http, HttpResponse } from 'msw';

import { server } from '@/testing/mocks/server';

import { getGroups } from '../get-groups';

const conversationId = '40c3cd3b-69d8-4af3-b1a7-f9174537fb97';

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
});
