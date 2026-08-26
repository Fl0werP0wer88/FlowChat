import { act, renderHook } from '@testing-library/react';

import {
  useConversationWorkspaceReconnectedSubscription,
  type UseConversationWorkspaceReconnectedSubscriptionOptions,
} from '../use-conversation-workspace-reconnected-subscription';

const realtime = vi.hoisted(() => ({
  handler: undefined as (() => void) | undefined,
  subscribe: vi.fn(),
  unsubscribe: vi.fn(),
}));

vi.mock('@/lib/realtime/realtime-client', () => ({
  subscribeToRealtimeReconnected: realtime.subscribe.mockImplementation((handler: () => void) => {
    realtime.handler = handler;
    return realtime.unsubscribe;
  }),
}));

const conversationId = '40c3cd3b-69d8-4af3-b1a7-f9174537fb97';
const otherConversationId = '3965a011-e9ec-4379-9b0b-e2d3132f67d8';

describe('useConversationWorkspaceReconnectedSubscription', () => {
  beforeEach(() => {
    realtime.handler = undefined;
    realtime.subscribe.mockClear();
    realtime.unsubscribe.mockClear();
  });

  it('reports reconnect for the active conversation and unsubscribes', () => {
    const onReconnected = vi.fn();
    const { unmount } = renderHook(() =>
      useConversationWorkspaceReconnectedSubscription({
        activeConversationId: conversationId,
        onReconnected,
      }),
    );

    act(() => realtime.handler?.());
    expect(onReconnected).toHaveBeenCalledWith(conversationId);
    unmount();
    expect(realtime.unsubscribe).toHaveBeenCalledOnce();
  });

  it('uses the latest options without subscribing again and ignores an inactive workspace', () => {
    const firstCallback = vi.fn();
    const nextCallback = vi.fn();
    const initialProps: UseConversationWorkspaceReconnectedSubscriptionOptions = {
      activeConversationId: conversationId,
      onReconnected: firstCallback,
    };
    const { rerender } = renderHook(
      (options: UseConversationWorkspaceReconnectedSubscriptionOptions) =>
        useConversationWorkspaceReconnectedSubscription(options),
      { initialProps },
    );

    rerender({ activeConversationId: otherConversationId, onReconnected: nextCallback });
    act(() => realtime.handler?.());
    rerender({ activeConversationId: null, onReconnected: nextCallback });
    act(() => realtime.handler?.());

    expect(realtime.subscribe).toHaveBeenCalledOnce();
    expect(firstCallback).not.toHaveBeenCalled();
    expect(nextCallback).toHaveBeenCalledOnce();
    expect(nextCallback).toHaveBeenCalledWith(otherConversationId);
  });
});
