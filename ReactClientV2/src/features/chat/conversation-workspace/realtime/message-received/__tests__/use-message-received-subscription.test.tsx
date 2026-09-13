import { QueryClientProvider } from '@tanstack/react-query';
import { act, renderHook } from '@testing-library/react';
import type { ReactNode } from 'react';

import { createTestQueryClient } from '@/testing/test-utils';

import {
  createConversationMessageBuffer,
  type ConversationMessageBufferState,
} from '../../../cache/conversation-message-buffer';
import { useMessageReceivedSubscription } from '../use-message-received-subscription';

const realtime = vi.hoisted(() => ({
  handler: undefined as ((payload: unknown) => void) | undefined,
  reconnectedHandler: undefined as (() => void) | undefined,
  subscribe: vi.fn(),
  subscribeToReconnected: vi.fn(),
  unsubscribe: vi.fn(),
  unsubscribeReconnected: vi.fn(),
  synchronizeQuery: vi.fn(() => Promise.resolve<number | null>(null)),
  synchronizeAll: vi.fn(() => Promise.resolve<Array<number | null>>([])),
}));

vi.mock('@/lib/realtime/realtime-client', () => ({
  subscribeToRealtimeEvent: realtime.subscribe.mockImplementation(
    (_eventName: string, handler: (payload: unknown) => void) => {
      realtime.handler = handler;
      return realtime.unsubscribe;
    },
  ),
  subscribeToRealtimeReconnected: realtime.subscribeToReconnected.mockImplementation(
    (handler: () => void) => {
      realtime.reconnectedHandler = handler;
      return realtime.unsubscribeReconnected;
    },
  ),
}));

vi.mock('../../../cache/use-conversation-message-sync', () => ({
  useConversationMessageSync: () => ({
    synchronizeQuery: realtime.synchronizeQuery,
    synchronizeAll: realtime.synchronizeAll,
  }),
}));

const conversationId = '40c3cd3b-69d8-4af3-b1a7-f9174537fb97';
const otherConversationId = '3965a011-e9ec-4379-9b0b-e2d3132f67d8';
const firstQueryKey = ['conversation-workspace', 'group', conversationId] as const;
const secondQueryKey = ['conversation-workspace', 'duet', 'partner-id', null] as const;
const otherQueryKey = ['conversation-workspace', 'group', otherConversationId] as const;

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
  return {
    conversationId: targetConversationId,
    ...createConversationMessageBuffer({ messages, currentSequenceNum: 2 }),
  };
}

function createWrapper(queryClient: ReturnType<typeof createTestQueryClient>) {
  return function Wrapper({ children }: { children: ReactNode }) {
    return <QueryClientProvider client={queryClient}>{children}</QueryClientProvider>;
  };
}

describe('useMessageReceivedSubscription', () => {
  beforeEach(() => {
    realtime.handler = undefined;
    realtime.reconnectedHandler = undefined;
    realtime.subscribe.mockClear();
    realtime.subscribeToReconnected.mockClear();
    realtime.unsubscribe.mockClear();
    realtime.unsubscribeReconnected.mockClear();
    realtime.synchronizeQuery.mockClear();
    realtime.synchronizeAll.mockClear();
  });

  it('updates every matching workspace cache and leaves other conversations unchanged', () => {
    const queryClient = createTestQueryClient();
    queryClient.setQueryData(firstQueryKey, createState());
    queryClient.setQueryData(secondQueryKey, createState());
    queryClient.setQueryData(otherQueryKey, createState(otherConversationId));
    const { unmount } = renderHook(() => useMessageReceivedSubscription(), {
      wrapper: createWrapper(queryClient),
    });

    act(() => realtime.handler?.(createPayload(3)));

    expect(realtime.subscribe).toHaveBeenCalledWith('MessageReceived', expect.any(Function));
    expect(
      queryClient.getQueryData<ConversationMessageBufferState>(firstQueryKey)
        ?.lastContiguousSequenceNum,
    ).toBe(3);
    expect(
      queryClient.getQueryData<ConversationMessageBufferState>(secondQueryKey)
        ?.lastContiguousSequenceNum,
    ).toBe(3);
    expect(
      queryClient.getQueryData<ConversationMessageBufferState>(otherQueryKey)
        ?.lastContiguousSequenceNum,
    ).toBe(2);

    unmount();
    expect(realtime.unsubscribe).toHaveBeenCalledOnce();
    expect(realtime.unsubscribeReconnected).toHaveBeenCalledOnce();
  });

  it('synchronizes a matching query after a gap and invalidates it after a conflict', () => {
    const queryClient = createTestQueryClient();
    queryClient.setQueryData(firstQueryKey, createState());
    const invalidateQueries = vi.spyOn(queryClient, 'invalidateQueries');
    renderHook(() => useMessageReceivedSubscription(), {
      wrapper: createWrapper(queryClient),
    });

    act(() => realtime.handler?.(createPayload(4)));
    expect(realtime.synchronizeQuery).toHaveBeenCalledWith(conversationId, firstQueryKey);

    act(() => realtime.handler?.(createPayload(2, conversationId, 99)));
    expect(invalidateQueries).toHaveBeenCalledWith({ queryKey: firstQueryKey, exact: true });
  });

  it('synchronizes all workspace caches after reconnect', () => {
    const queryClient = createTestQueryClient();
    renderHook(() => useMessageReceivedSubscription(), {
      wrapper: createWrapper(queryClient),
    });

    act(() => realtime.reconnectedHandler?.());

    expect(realtime.synchronizeAll).toHaveBeenCalledOnce();
  });

  it('ignores malformed events and valid events without matching cache', () => {
    const queryClient = createTestQueryClient();
    const setQueryData = vi.spyOn(queryClient, 'setQueryData');
    renderHook(() => useMessageReceivedSubscription(), {
      wrapper: createWrapper(queryClient),
    });

    act(() => {
      realtime.handler?.({ ...createPayload(3), sequenceNum: -1 });
      realtime.handler?.(createPayload(3));
    });

    expect(setQueryData).not.toHaveBeenCalled();
    expect(realtime.synchronizeQuery).not.toHaveBeenCalled();
  });
});
