import { QueryClientProvider } from '@tanstack/react-query';
import { act, renderHook } from '@testing-library/react';
import type { ReactNode } from 'react';

import { createTestQueryClient } from '@/testing/test-utils';

import {
  createConversationMessageBuffer,
  type ConversationMessageBufferState,
} from '../../../cache/conversation-message-buffer';
import {
  useMessageReceivedSubscription,
  type UseMessageReceivedSubscriptionOptions,
} from '../use-message-received-subscription';

const realtime = vi.hoisted(() => ({
  handler: undefined as ((payload: unknown) => void) | undefined,
  subscribe: vi.fn(),
  unsubscribe: vi.fn(),
}));

vi.mock('@/lib/realtime/realtime-client', () => ({
  subscribeToRealtimeEvent: realtime.subscribe.mockImplementation(
    (_eventName: string, handler: (payload: unknown) => void) => {
      realtime.handler = handler;
      return realtime.unsubscribe;
    },
  ),
}));

const conversationId = '40c3cd3b-69d8-4af3-b1a7-f9174537fb97';
const otherConversationId = '3965a011-e9ec-4379-9b0b-e2d3132f67d8';
const queryKey = ['conversation-workspace', conversationId] as const;
const otherQueryKey = ['conversation-workspace', otherConversationId] as const;

function createPayload(
  sequenceNum: number,
  targetConversationId = conversationId,
  idSuffix = sequenceNum,
) {
  return {
    messageId: `00000000-0000-4000-8000-${idSuffix.toString().padStart(12, '0')}`,
    conversationId: targetConversationId,
    senderUserId: '91f65d44-d175-45af-8839-d2d36e7d61f9',
    text: `Message ${sequenceNum}`,
    sequenceNum,
    sentAtUtc: `2026-08-24T10:00:${sequenceNum.toString().padStart(2, '0')}+00:00`,
    deliveredAtUtc: `2026-08-24T10:01:${sequenceNum.toString().padStart(2, '0')}+00:00`,
  };
}

function createState(targetConversationId = conversationId) {
  const first = createPayload(1, targetConversationId);
  const second = createPayload(2, targetConversationId);
  return createConversationMessageBuffer({
    messages: [
      {
        id: first.messageId,
        conversationId: first.conversationId,
        senderUserId: first.senderUserId,
        text: first.text,
        sequenceNum: first.sequenceNum,
        sentAtUtc: first.sentAtUtc,
      },
      {
        id: second.messageId,
        conversationId: second.conversationId,
        senderUserId: second.senderUserId,
        text: second.text,
        sequenceNum: second.sequenceNum,
        sentAtUtc: second.sentAtUtc,
      },
    ],
    currentSequenceNum: 2,
  });
}

function createWrapper(queryClient: ReturnType<typeof createTestQueryClient>) {
  return function Wrapper({ children }: { children: ReactNode }) {
    return <QueryClientProvider client={queryClient}>{children}</QueryClientProvider>;
  };
}

describe('useMessageReceivedSubscription', () => {
  beforeEach(() => {
    realtime.handler = undefined;
    realtime.subscribe.mockClear();
    realtime.unsubscribe.mockClear();
  });

  it('updates the active cache for a valid event and unsubscribes', () => {
    const queryClient = createTestQueryClient();
    const synchronizeMessages = vi.fn(() => Promise.resolve<number | null>(null));
    queryClient.setQueryData(queryKey, createState());
    const { unmount } = renderHook(
      () =>
        useMessageReceivedSubscription({
          activeConversationId: conversationId,
          queryKey,
          enabled: true,
          synchronizeMessages,
        }),
      { wrapper: createWrapper(queryClient) },
    );

    act(() => {
      realtime.handler?.(createPayload(3));
      realtime.handler?.(createPayload(4, otherConversationId));
      realtime.handler?.({ ...createPayload(4), sequenceNum: -1 });
    });

    expect(realtime.subscribe).toHaveBeenCalledWith('MessageReceived', expect.any(Function));
    expect(
      queryClient.getQueryData<ConversationMessageBufferState>(queryKey)?.lastContiguousSequenceNum,
    ).toBe(3);
    expect(synchronizeMessages).not.toHaveBeenCalled();
    unmount();
    expect(realtime.unsubscribe).toHaveBeenCalledOnce();
  });

  it('starts catch-up for a gap and invalidates the query for a conflict', () => {
    const queryClient = createTestQueryClient();
    const synchronizeMessages = vi.fn(() => Promise.resolve<number | null>(null));
    const invalidateQueries = vi.spyOn(queryClient, 'invalidateQueries');
    queryClient.setQueryData(queryKey, createState());
    renderHook(
      () =>
        useMessageReceivedSubscription({
          activeConversationId: conversationId,
          queryKey,
          enabled: true,
          synchronizeMessages,
        }),
      { wrapper: createWrapper(queryClient) },
    );

    act(() => realtime.handler?.(createPayload(4)));
    expect(synchronizeMessages).toHaveBeenCalledOnce();

    act(() => realtime.handler?.(createPayload(2, conversationId, 99)));
    expect(invalidateQueries).toHaveBeenCalledWith({ queryKey, exact: true });
  });

  it('uses the latest query configuration without subscribing again', () => {
    const queryClient = createTestQueryClient();
    const firstSynchronize = vi.fn(() => Promise.resolve<number | null>(null));
    const nextSynchronize = vi.fn(() => Promise.resolve<number | null>(null));
    queryClient.setQueryData(queryKey, createState());
    queryClient.setQueryData(otherQueryKey, createState(otherConversationId));
    const initialProps: UseMessageReceivedSubscriptionOptions = {
      activeConversationId: conversationId,
      queryKey,
      enabled: true,
      synchronizeMessages: firstSynchronize,
    };
    const { rerender } = renderHook(
      (options: UseMessageReceivedSubscriptionOptions) => useMessageReceivedSubscription(options),
      { initialProps, wrapper: createWrapper(queryClient) },
    );

    rerender({
      activeConversationId: otherConversationId,
      queryKey: otherQueryKey,
      enabled: true,
      synchronizeMessages: nextSynchronize,
    });
    act(() => realtime.handler?.(createPayload(4, otherConversationId)));

    expect(realtime.subscribe).toHaveBeenCalledOnce();
    expect(firstSynchronize).not.toHaveBeenCalled();
    expect(nextSynchronize).toHaveBeenCalledOnce();
    expect(
      queryClient.getQueryData<ConversationMessageBufferState>(otherQueryKey)
        ?.pendingMessagesBySequence,
    ).toHaveProperty('4');

    rerender({
      activeConversationId: otherConversationId,
      queryKey: otherQueryKey,
      enabled: false,
      synchronizeMessages: nextSynchronize,
    });
    act(() => realtime.handler?.(createPayload(3, otherConversationId)));
    expect(
      queryClient.getQueryData<ConversationMessageBufferState>(otherQueryKey)
        ?.lastContiguousSequenceNum,
    ).toBe(2);
  });
});
