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
  messageSyncOptions: undefined as UseMessageReceivedSubscriptionOptions | undefined,
  subscribe: vi.fn(),
  unsubscribe: vi.fn(),
  synchronizeMessages: vi.fn(() => Promise.resolve<number | null>(null)),
  useMessageSync: vi.fn(),
}));

vi.mock('@/lib/realtime/realtime-client', () => ({
  subscribeToRealtimeEvent: realtime.subscribe.mockImplementation(
    (_eventName: string, handler: (payload: unknown) => void) => {
      realtime.handler = handler;
      return realtime.unsubscribe;
    },
  ),
}));

vi.mock('../../../cache/use-conversation-message-sync', () => ({
  useConversationMessageSync: realtime.useMessageSync.mockImplementation(
    (options: UseMessageReceivedSubscriptionOptions) => {
      realtime.messageSyncOptions = options;
      return realtime.synchronizeMessages;
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
  const messages = [1, 2].map((sequenceNum) => {
    const payload = createPayload(sequenceNum, targetConversationId);
    return {
      id: payload.messageId,
      conversationId: payload.conversationId,
      senderUserId: payload.senderUserId,
      text: payload.text,
      sequenceNum: payload.sequenceNum,
      sentAtUtc: payload.sentAtUtc,
    };
  });
  return createConversationMessageBuffer({ messages, currentSequenceNum: 2 });
}

function createWrapper(queryClient: ReturnType<typeof createTestQueryClient>) {
  return function Wrapper({ children }: { children: ReactNode }) {
    return <QueryClientProvider client={queryClient}>{children}</QueryClientProvider>;
  };
}

describe('useMessageReceivedSubscription', () => {
  beforeEach(() => {
    realtime.handler = undefined;
    realtime.messageSyncOptions = undefined;
    realtime.subscribe.mockClear();
    realtime.unsubscribe.mockClear();
    realtime.synchronizeMessages.mockClear();
    realtime.useMessageSync.mockClear();
  });

  it('owns message synchronization and updates the active cache', () => {
    const queryClient = createTestQueryClient();
    queryClient.setQueryData(queryKey, createState());
    const { result, unmount } = renderHook(
      () => useMessageReceivedSubscription({ conversationId, queryKey, enabled: true }),
      { wrapper: createWrapper(queryClient) },
    );

    act(() => {
      realtime.handler?.(createPayload(3));
      realtime.handler?.(createPayload(4, otherConversationId));
      realtime.handler?.({ ...createPayload(4), sequenceNum: -1 });
    });

    expect(result.current).toBe(realtime.synchronizeMessages);
    expect(realtime.messageSyncOptions).toEqual({ conversationId, queryKey, enabled: true });
    expect(realtime.subscribe).toHaveBeenCalledWith('MessageReceived', expect.any(Function));
    expect(
      queryClient.getQueryData<ConversationMessageBufferState>(queryKey)?.lastContiguousSequenceNum,
    ).toBe(3);
    expect(realtime.synchronizeMessages).not.toHaveBeenCalled();
    unmount();
    expect(realtime.unsubscribe).toHaveBeenCalledOnce();
  });

  it('starts its synchronizer for a gap and invalidates the query for a conflict', () => {
    const queryClient = createTestQueryClient();
    const invalidateQueries = vi.spyOn(queryClient, 'invalidateQueries');
    queryClient.setQueryData(queryKey, createState());
    renderHook(() => useMessageReceivedSubscription({ conversationId, queryKey, enabled: true }), {
      wrapper: createWrapper(queryClient),
    });

    act(() => realtime.handler?.(createPayload(4)));
    expect(realtime.synchronizeMessages).toHaveBeenCalledOnce();

    act(() => realtime.handler?.(createPayload(2, conversationId, 99)));
    expect(invalidateQueries).toHaveBeenCalledWith({ queryKey, exact: true });
  });

  it('uses the latest conversation and query without subscribing again', () => {
    const queryClient = createTestQueryClient();
    queryClient.setQueryData(queryKey, createState());
    queryClient.setQueryData(otherQueryKey, createState(otherConversationId));
    const initialProps: UseMessageReceivedSubscriptionOptions = {
      conversationId,
      queryKey,
      enabled: true,
    };
    const { rerender } = renderHook(
      (options: UseMessageReceivedSubscriptionOptions) => useMessageReceivedSubscription(options),
      { initialProps, wrapper: createWrapper(queryClient) },
    );

    rerender({ conversationId: otherConversationId, queryKey: otherQueryKey, enabled: true });
    act(() => realtime.handler?.(createPayload(4, otherConversationId)));

    expect(realtime.subscribe).toHaveBeenCalledOnce();
    expect(realtime.synchronizeMessages).toHaveBeenCalledOnce();
    expect(realtime.messageSyncOptions).toEqual({
      conversationId: otherConversationId,
      queryKey: otherQueryKey,
      enabled: true,
    });
    expect(
      queryClient.getQueryData<ConversationMessageBufferState>(otherQueryKey)
        ?.pendingMessagesBySequence,
    ).toHaveProperty('4');

    rerender({ conversationId: otherConversationId, queryKey: otherQueryKey, enabled: false });
    act(() => realtime.handler?.(createPayload(3, otherConversationId)));
    expect(
      queryClient.getQueryData<ConversationMessageBufferState>(otherQueryKey)
        ?.lastContiguousSequenceNum,
    ).toBe(2);
  });
});
