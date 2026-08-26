import { QueryClientProvider } from '@tanstack/react-query';
import { act, renderHook } from '@testing-library/react';
import type { ReactNode } from 'react';

import { createTestQueryClient } from '@/testing/test-utils';

import { useConversationWorkspaceRealtimeSync } from '../use-conversation-workspace-realtime-sync';
import type { UseConversationWorkspaceSubscriptionOptions } from '../use-conversation-workspace-subscription';

const mocks = vi.hoisted(() => ({
  subscriptionOptions: undefined as UseConversationWorkspaceSubscriptionOptions | undefined,
  synchronizeMessages: vi.fn(() => Promise.resolve<number | null>(null)),
}));

vi.mock('../../cache/use-conversation-message-sync', () => ({
  useConversationMessageSync: () => mocks.synchronizeMessages,
}));

vi.mock('../use-conversation-workspace-subscription', () => ({
  useConversationWorkspaceSubscription: (options: UseConversationWorkspaceSubscriptionOptions) => {
    mocks.subscriptionOptions = options;
  },
}));

const conversationId = '40c3cd3b-69d8-4af3-b1a7-f9174537fb97';
const queryKey = ['conversation-workspace', 'group', conversationId] as const;

function createWrapper(queryClient: ReturnType<typeof createTestQueryClient>) {
  return function Wrapper({ children }: { children: ReactNode }) {
    return <QueryClientProvider client={queryClient}>{children}</QueryClientProvider>;
  };
}

describe('useConversationWorkspaceRealtimeSync', () => {
  beforeEach(() => {
    mocks.subscriptionOptions = undefined;
    mocks.synchronizeMessages.mockClear();
  });

  it('configures workspace subscriptions with the message synchronizer', () => {
    const queryClient = createTestQueryClient();
    const { result } = renderHook(
      () => useConversationWorkspaceRealtimeSync({ conversationId, queryKey, enabled: true }),
      { wrapper: createWrapper(queryClient) },
    );

    expect(result.current).toBe(mocks.synchronizeMessages);
    expect(mocks.subscriptionOptions).toMatchObject({
      activeConversationId: conversationId,
      queryKey,
      enabled: true,
      synchronizeMessages: mocks.synchronizeMessages,
    });
  });

  it('synchronizes after reconnect and forwards metadata callbacks', () => {
    const queryClient = createTestQueryClient();
    const onGroupConversationChanged = vi.fn();
    const onParticipantsAdded = vi.fn();
    const onParticipantsRemoved = vi.fn();
    renderHook(
      () =>
        useConversationWorkspaceRealtimeSync({
          conversationId,
          queryKey,
          enabled: true,
          onGroupConversationChanged,
          onParticipantsAdded,
          onParticipantsRemoved,
        }),
      { wrapper: createWrapper(queryClient) },
    );

    act(() => mocks.subscriptionOptions?.onReconnected?.(conversationId));

    expect(mocks.synchronizeMessages).toHaveBeenCalledOnce();
    expect(mocks.subscriptionOptions).toMatchObject({
      activeConversationId: conversationId,
      onGroupConversationChanged,
      onParticipantsAdded,
      onParticipantsRemoved,
    });
  });

  it('disables the active realtime conversation when synchronization is disabled', () => {
    const queryClient = createTestQueryClient();
    renderHook(
      () => useConversationWorkspaceRealtimeSync({ conversationId, queryKey, enabled: false }),
      { wrapper: createWrapper(queryClient) },
    );

    expect(mocks.subscriptionOptions?.activeConversationId).toBeNull();
    expect(mocks.subscriptionOptions?.enabled).toBe(false);
    expect(mocks.synchronizeMessages).not.toHaveBeenCalled();
  });
});
