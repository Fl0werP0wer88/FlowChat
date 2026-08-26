import { act, renderHook } from '@testing-library/react';

import { useConversationParticipantsRemovedSubscription } from '../use-conversation-participants-removed-subscription';

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

describe('useConversationParticipantsRemovedSubscription', () => {
  beforeEach(() => {
    realtime.handler = undefined;
    realtime.subscribe.mockClear();
    realtime.unsubscribe.mockClear();
  });

  it('routes only valid events for the active conversation and unsubscribes', () => {
    const onParticipantsRemoved = vi.fn();
    const { unmount } = renderHook(() =>
      useConversationParticipantsRemovedSubscription({
        activeConversationId: conversationId,
        onParticipantsRemoved,
      }),
    );

    act(() => {
      realtime.handler?.(createPayload());
      realtime.handler?.(createPayload(otherConversationId));
      realtime.handler?.({ ...createPayload(), participantUserIds: ['invalid'] });
    });

    expect(realtime.subscribe).toHaveBeenCalledWith(
      'ConversationParticipantsRemoved',
      expect.any(Function),
    );
    expect(onParticipantsRemoved).toHaveBeenCalledOnce();
    unmount();
    expect(realtime.unsubscribe).toHaveBeenCalledOnce();
  });

  it('uses the latest conversation and callback without subscribing again', () => {
    const firstCallback = vi.fn();
    const nextCallback = vi.fn();
    const { rerender } = renderHook(
      ({ activeConversationId, onParticipantsRemoved }) =>
        useConversationParticipantsRemovedSubscription({
          activeConversationId,
          onParticipantsRemoved,
        }),
      {
        initialProps: {
          activeConversationId: conversationId,
          onParticipantsRemoved: firstCallback,
        },
      },
    );

    rerender({ activeConversationId: otherConversationId, onParticipantsRemoved: nextCallback });
    act(() => realtime.handler?.(createPayload(otherConversationId)));

    expect(realtime.subscribe).toHaveBeenCalledOnce();
    expect(firstCallback).not.toHaveBeenCalled();
    expect(nextCallback).toHaveBeenCalledOnce();
  });
});
