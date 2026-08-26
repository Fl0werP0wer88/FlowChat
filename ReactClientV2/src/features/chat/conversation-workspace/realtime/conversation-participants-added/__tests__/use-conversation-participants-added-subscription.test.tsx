import { act, renderHook } from '@testing-library/react';

import { useConversationParticipantsAddedSubscription } from '../use-conversation-participants-added-subscription';

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
const participantUserId = '14c11faa-8bd7-4608-abcf-26985f3f62be';

function createPayload(targetConversationId = conversationId) {
  return {
    conversationId: targetConversationId,
    conversationType: 2,
    participantUserIds: [participantUserId],
  };
}

describe('useConversationParticipantsAddedSubscription', () => {
  beforeEach(() => {
    realtime.handler = undefined;
    realtime.subscribe.mockClear();
    realtime.unsubscribe.mockClear();
  });

  it('routes only valid events for the active conversation and unsubscribes', () => {
    const onParticipantsAdded = vi.fn();
    const { unmount } = renderHook(() =>
      useConversationParticipantsAddedSubscription({
        activeConversationId: conversationId,
        onParticipantsAdded,
      }),
    );

    act(() => {
      realtime.handler?.(createPayload());
      realtime.handler?.(createPayload(otherConversationId));
      realtime.handler?.({ ...createPayload(), conversationType: 3 });
    });

    expect(realtime.subscribe).toHaveBeenCalledWith(
      'ConversationParticipantsAdded',
      expect.any(Function),
    );
    expect(onParticipantsAdded).toHaveBeenCalledOnce();
    unmount();
    expect(realtime.unsubscribe).toHaveBeenCalledOnce();
  });

  it('uses the latest conversation and callback without subscribing again', () => {
    const firstCallback = vi.fn();
    const nextCallback = vi.fn();
    const { rerender } = renderHook(
      ({ activeConversationId, onParticipantsAdded }) =>
        useConversationParticipantsAddedSubscription({
          activeConversationId,
          onParticipantsAdded,
        }),
      {
        initialProps: {
          activeConversationId: conversationId,
          onParticipantsAdded: firstCallback,
        },
      },
    );

    rerender({ activeConversationId: otherConversationId, onParticipantsAdded: nextCallback });
    act(() => realtime.handler?.(createPayload(otherConversationId)));

    expect(realtime.subscribe).toHaveBeenCalledOnce();
    expect(firstCallback).not.toHaveBeenCalled();
    expect(nextCallback).toHaveBeenCalledOnce();
  });
});
