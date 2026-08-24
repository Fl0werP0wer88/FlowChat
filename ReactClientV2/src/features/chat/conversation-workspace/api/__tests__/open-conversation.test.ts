import { http, HttpResponse } from 'msw';

import { server } from '@/testing/mocks/server';

import { openDuetConversation, openDuetConversationQueryOptions } from '../open-duet-conversation';
import {
  openGroupConversation,
  openGroupConversationQueryOptions,
} from '../open-group-conversation';

const conversationId = '40c3cd3b-69d8-4af3-b1a7-f9174537fb97';
const partnerUserId = '14c11faa-8bd7-4608-abcf-26985f3f62be';
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

function createParticipant() {
  return {
    userId: partnerUserId,
    displayName: 'Alex Morgan',
    avatarUrl: null,
    participantUserId: partnerUserId,
  };
}

describe('open conversation API', () => {
  it('opens a duet without a known conversation and preserves message order', async () => {
    let requestPayload: Record<string, unknown> = {};
    server.use(
      http.put('*/api/aggregate/conversations/duet/open', async ({ request }) => {
        requestPayload = (await request.json()) as Record<string, unknown>;
        return HttpResponse.json({
          conversationId,
          participants: [createParticipant()],
          messages: [createMessage(2), createMessage(1)],
          nextBeforeSequenceNum: 1,
          currentSequenceNum: 2,
          hasMore: true,
        });
      }),
    );

    const result = await openDuetConversation({ partnerUserId });

    expect(requestPayload).toEqual({ partnerUserId, knownConversationId: null });
    expect(result.messages.map((message) => message.sequenceNum)).toEqual([2, 1]);
    expect(openDuetConversationQueryOptions({ partnerUserId }).queryKey).toEqual([
      'conversation-workspace',
      'duet',
      partnerUserId,
      null,
    ]);
  });

  it('opens a duet with a known conversation id', async () => {
    let requestPayload: Record<string, unknown> = {};
    server.use(
      http.put('*/api/aggregate/conversations/duet/open', async ({ request }) => {
        requestPayload = (await request.json()) as Record<string, unknown>;
        return HttpResponse.json({
          conversationId,
          participants: [createParticipant()],
          messages: [],
          nextBeforeSequenceNum: null,
          currentSequenceNum: 0,
          hasMore: false,
        });
      }),
    );

    await openDuetConversation({ partnerUserId, knownConversationId: conversationId });

    expect(requestPayload).toEqual({ partnerUserId, knownConversationId: conversationId });
  });

  it('opens a group and includes its conversation id in the query key', async () => {
    let requestPayload: Record<string, unknown> = {};
    server.use(
      http.put('*/api/aggregate/conversations/group/open', async ({ request }) => {
        requestPayload = (await request.json()) as Record<string, unknown>;
        return HttpResponse.json({
          conversationId,
          name: 'Product team',
          participants: [createParticipant()],
          messages: [createMessage(1)],
          nextBeforeSequenceNum: null,
          currentSequenceNum: 1,
          hasMore: false,
        });
      }),
    );

    const result = await openGroupConversation({ conversationId });

    expect(requestPayload).toEqual({ conversationId });
    expect(result.name).toBe('Product team');
    expect(openGroupConversationQueryOptions({ conversationId }).queryKey).toEqual([
      'conversation-workspace',
      'group',
      conversationId,
    ]);
  });

  it('rejects invalid input and an invalid success response', async () => {
    await expect(openDuetConversation({ partnerUserId: 'invalid' })).rejects.toBeDefined();

    server.use(
      http.put('*/api/aggregate/conversations/group/open', () =>
        HttpResponse.json({
          conversationId,
          name: 'Product team',
          participants: [{ ...createParticipant(), userId: 'invalid' }],
          messages: [],
          nextBeforeSequenceNum: null,
          currentSequenceNum: 0,
          hasMore: false,
        }),
      ),
    );

    await expect(openGroupConversation({ conversationId })).rejects.toBeDefined();
  });

  it('cancels an in-flight open request', async () => {
    server.use(
      http.put('*/api/aggregate/conversations/duet/open', async () => {
        await new Promise((resolve) => window.setTimeout(resolve, 1_000));
        return HttpResponse.json({});
      }),
    );
    const abortController = new AbortController();

    const request = openDuetConversation({ partnerUserId }, abortController.signal);
    abortController.abort();

    await expect(request).rejects.toBeDefined();
  });
});
