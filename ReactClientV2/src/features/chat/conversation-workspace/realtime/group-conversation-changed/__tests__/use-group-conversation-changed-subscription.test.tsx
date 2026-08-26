import { act, renderHook } from '@testing-library/react';

import { useGroupConversationChangedSubscription } from '../use-group-conversation-changed-subscription';

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
  return { conversationId: targetConversationId, type: 2, name: 'Product team' };
}

describe('useGroupConversationChangedSubscription', () => {
  beforeEach(() => {
    realtime.handler = undefined;
    realtime.subscribe.mockClear();
    realtime.unsubscribe.mockClear();
  });

  it('routes only valid events for the active conversation and unsubscribes', () => {
    const onGroupConversationChanged = vi.fn();
    const { unmount } = renderHook(() =>
      useGroupConversationChangedSubscription({
        activeConversationId: conversationId,
        onGroupConversationChanged,
      }),
    );

    act(() => {
      realtime.handler?.(createPayload());
      realtime.handler?.(createPayload(otherConversationId));
      realtime.handler?.({ ...createPayload(), type: 1 });
    });

    expect(realtime.subscribe).toHaveBeenCalledWith(
      'GroupConversationChanged',
      expect.any(Function),
    );
    expect(onGroupConversationChanged).toHaveBeenCalledOnce();
    unmount();
    expect(realtime.unsubscribe).toHaveBeenCalledOnce();
  });

  it('uses the latest conversation and callback without subscribing again', () => {
    const firstCallback = vi.fn();
    const nextCallback = vi.fn();
    const { rerender } = renderHook(
      ({ activeConversationId, onGroupConversationChanged }) =>
        useGroupConversationChangedSubscription({
          activeConversationId,
          onGroupConversationChanged,
        }),
      {
        initialProps: {
          activeConversationId: conversationId,
          onGroupConversationChanged: firstCallback,
        },
      },
    );

    rerender({
      activeConversationId: otherConversationId,
      onGroupConversationChanged: nextCallback,
    });
    act(() => realtime.handler?.(createPayload(otherConversationId)));

    expect(realtime.subscribe).toHaveBeenCalledOnce();
    expect(firstCallback).not.toHaveBeenCalled();
    expect(nextCallback).toHaveBeenCalledOnce();
  });
});
