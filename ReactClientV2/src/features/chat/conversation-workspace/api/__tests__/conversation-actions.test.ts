import { http, HttpResponse } from 'msw';

import { server } from '@/testing/mocks/server';

import { markConversationAsRead } from '../mark-conversation-as-read';
import { sendChatMessage } from '../send-chat-message';

const conversationId = '40c3cd3b-69d8-4af3-b1a7-f9174537fb97';
const messageId = 'c7d063d8-0db1-4adf-a425-01f9000b8ace';

describe('conversation actions API', () => {
  it('sends a message using a caller-provided id', async () => {
    let requestPayload: Record<string, unknown> = {};
    server.use(
      http.put('*/api/chat/messages', async ({ request }) => {
        requestPayload = (await request.json()) as Record<string, unknown>;
        return HttpResponse.json(
          {
            messageId,
            sentAtUtc: '2026-08-24T10:00:00+00:00',
            sequenceNum: 7,
          },
          { status: 201 },
        );
      }),
    );

    const result = await sendChatMessage({
      data: { id: messageId, conversationId, text: '  Hello  ' },
    });

    expect(requestPayload).toEqual({ id: messageId, conversationId, text: 'Hello' });
    expect(result).toEqual({
      messageId,
      sentAtUtc: '2026-08-24T10:00:00+00:00',
      sequenceNum: 7,
    });
  });

  it('marks a conversation as read and accepts a 204 response', async () => {
    let requestPayload: Record<string, unknown> = {};
    let requestedConversationId = '';
    server.use(
      http.put('*/api/conversations/:conversationId/read-state', async ({ params, request }) => {
        requestedConversationId = String(params.conversationId);
        requestPayload = (await request.json()) as Record<string, unknown>;
        return new HttpResponse(null, { status: 204 });
      }),
    );

    await expect(
      markConversationAsRead({ data: { conversationId, sequenceNum: 7 } }),
    ).resolves.toBeUndefined();
    expect(requestedConversationId).toBe(conversationId);
    expect(requestPayload).toEqual({ sequenceNum: 7 });
  });

  it('rejects invalid action input', async () => {
    await expect(
      sendChatMessage({ data: { id: messageId, conversationId, text: '   ' } }),
    ).rejects.toBeDefined();
    await expect(
      markConversationAsRead({ data: { conversationId, sequenceNum: -1 } }),
    ).rejects.toBeDefined();
  });

  it('rejects a malformed send response', async () => {
    server.use(
      http.put('*/api/chat/messages', () =>
        HttpResponse.json(
          {
            messageId,
            sentAtUtc: 'invalid',
            sequenceNum: 1,
          },
          { status: 201 },
        ),
      ),
    );

    await expect(
      sendChatMessage({ data: { id: messageId, conversationId, text: 'Hello' } }),
    ).rejects.toBeDefined();
  });
});
