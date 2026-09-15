import { http, HttpResponse } from 'msw';

import { server } from '@/testing/mocks/server';

import { catchUpConversationMessages } from '../catch-up-conversation-messages';
import { getConversationMessages } from '../get-conversation-messages';

const conversationId = '40c3cd3b-69d8-4af3-b1a7-f9174537fb97';
const senderUserId = '91f65d44-d175-45af-8839-d2d36e7d61f9';
const messageId = 'c7d063d8-0db1-4adf-a425-01f9000b8ace';

function createMessage(sequenceNum: number) {
  return {
    id: messageId,
    conversationId,
    senderUserId,
    text: `Message ${sequenceNum}`,
    sentAtUtc: '2026-08-24T10:00:00+00:00',
    sequenceNum,
  };
}

describe('conversation messages API', () => {
  it('gets an older page with the V1-compatible default limit', async () => {
    let requestUrl = '';
    server.use(
      http.get('*/api/chat/conversations/:conversationId/messages', ({ request }) => {
        requestUrl = request.url;
        return HttpResponse.json({
          items: [createMessage(19), createMessage(18)],
          nextBeforeSequenceNum: 18,
          currentSequenceNum: 25,
          hasMore: true,
        });
      }),
    );

    const result = await getConversationMessages({ conversationId, beforeSequenceNum: 20 });
    const searchParams = new URL(requestUrl).searchParams;

    expect(searchParams.get('limit')).toBe('10');
    expect(searchParams.get('beforeSequenceNum')).toBe('20');
    expect(result.items.map((message) => message.sequenceNum)).toEqual([19, 18]);
  });

  it('gets a catch-up page with its boundary and default limit', async () => {
    let requestUrl = '';
    server.use(
      http.get('*/api/chat/conversations/:conversationId/messages/catch-up', ({ request }) => {
        requestUrl = request.url;
        return HttpResponse.json({
          items: [createMessage(11), createMessage(12)],
          nextAfterSequenceNum: null,
          currentSequenceNum: 15,
          throughSequenceNum: 15,
          hasMore: false,
        });
      }),
    );

    const result = await catchUpConversationMessages({
      conversationId,
      afterSequenceNum: 10,
      throughSequenceNum: 15,
    });
    const searchParams = new URL(requestUrl).searchParams;

    expect(searchParams.get('afterSequenceNum')).toBe('10');
    expect(searchParams.get('throughSequenceNum')).toBe('15');
    expect(searchParams.get('limit')).toBe('100');
    expect(result.items.map((message) => message.sequenceNum)).toEqual([11, 12]);
  });

  it('rejects invalid parameters and malformed messages', async () => {
    await expect(getConversationMessages({ conversationId, limit: 101 })).rejects.toBeDefined();
    await expect(
      catchUpConversationMessages({ conversationId, afterSequenceNum: -1 }),
    ).rejects.toBeDefined();

    server.use(
      http.get('*/api/chat/conversations/:conversationId/messages', () =>
        HttpResponse.json({
          items: [{ ...createMessage(1), sentAtUtc: 'not-a-date' }],
          nextBeforeSequenceNum: null,
          currentSequenceNum: 1,
          hasMore: false,
        }),
      ),
    );

    await expect(getConversationMessages({ conversationId })).rejects.toBeDefined();
  });

  it.each([
    {
      name: 'history',
      path: '*/api/chat/conversations/:conversationId/messages',
      request: (signal: AbortSignal) => getConversationMessages({ conversationId }, signal),
    },
    {
      name: 'catch-up',
      path: '*/api/chat/conversations/:conversationId/messages/catch-up',
      request: (signal: AbortSignal) =>
        catchUpConversationMessages({ conversationId, afterSequenceNum: 0 }, signal),
    },
  ])('cancels an in-flight $name request', async ({ path, request }) => {
    server.use(
      http.get(path, async () => {
        await new Promise((resolve) => window.setTimeout(resolve, 1_000));
        return HttpResponse.json({});
      }),
    );
    const abortController = new AbortController();

    const pendingRequest = request(abortController.signal);
    abortController.abort();

    await expect(pendingRequest).rejects.toBeDefined();
  });
});
