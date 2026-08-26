import { act, renderHook } from '@testing-library/react';

import { useMessageReceivedSubscription } from '../use-message-received-subscription';

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

function createPayload(targetConversationId = conversationId) {
  return {
    messageId: 'c7d063d8-0db1-4adf-a425-01f9000b8ace',
    conversationId: targetConversationId,
    senderUserId: '91f65d44-d175-45af-8839-d2d36e7d61f9',
    text: 'Hello',
    sequenceNum: 7,
    sentAtUtc: '2026-08-24T10:00:00+00:00',
    deliveredAtUtc: '2026-08-24T10:00:01+00:00',
  };
}

describe('useMessageReceivedSubscription', () => {
  beforeEach(() => {
    realtime.handler = undefined;
    realtime.subscribe.mockClear();
    realtime.unsubscribe.mockClear();
  });

  it('routes only valid events for the active conversation and unsubscribes', () => {
    const onMessageReceived = vi.fn();
    const { unmount } = renderHook(() =>
      useMessageReceivedSubscription({ activeConversationId: conversationId, onMessageReceived }),
    );

    act(() => {
      realtime.handler?.(createPayload());
      realtime.handler?.(createPayload(otherConversationId));
      realtime.handler?.({ ...createPayload(), sequenceNum: -1 });
    });

    expect(realtime.subscribe).toHaveBeenCalledWith('MessageReceived', expect.any(Function));
    expect(onMessageReceived).toHaveBeenCalledOnce();
    unmount();
    expect(realtime.unsubscribe).toHaveBeenCalledOnce();
  });

  it('uses the latest conversation and callback without subscribing again', () => {
    const firstCallback = vi.fn();
    const nextCallback = vi.fn();
    const { rerender } = renderHook(
      ({ activeConversationId, onMessageReceived }) =>
        useMessageReceivedSubscription({ activeConversationId, onMessageReceived }),
      { initialProps: { activeConversationId: conversationId, onMessageReceived: firstCallback } },
    );

    rerender({ activeConversationId: otherConversationId, onMessageReceived: nextCallback });
    act(() => realtime.handler?.(createPayload(otherConversationId)));

    expect(realtime.subscribe).toHaveBeenCalledOnce();
    expect(firstCallback).not.toHaveBeenCalled();
    expect(nextCallback).toHaveBeenCalledOnce();
  });
});
