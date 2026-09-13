import { QueryClientProvider } from '@tanstack/react-query';
import { act, renderHook } from '@testing-library/react';
import type { ReactNode } from 'react';

import { getGroupsQueryOptions } from '@/features/chat/conversations/group/api/get-groups';
import { createTestQueryClient } from '@/testing/test-utils';

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
const workspaceQueryKey = ['conversation-workspace', 'group', conversationId] as const;
const otherWorkspaceQueryKey = ['conversation-workspace', 'group', otherConversationId] as const;

function createWrapper(queryClient: ReturnType<typeof createTestQueryClient>) {
  return function Wrapper({ children }: { children: ReactNode }) {
    return <QueryClientProvider client={queryClient}>{children}</QueryClientProvider>;
  };
}

describe('useGroupConversationChangedSubscription', () => {
  beforeEach(() => {
    realtime.handler = undefined;
    realtime.subscribe.mockClear();
    realtime.unsubscribe.mockClear();
  });

  it('invalidates the matching workspace and group list', () => {
    const queryClient = createTestQueryClient();
    queryClient.setQueryData(workspaceQueryKey, { conversationId });
    queryClient.setQueryData(otherWorkspaceQueryKey, { conversationId: otherConversationId });
    const invalidateQueries = vi.spyOn(queryClient, 'invalidateQueries');
    const { unmount } = renderHook(() => useGroupConversationChangedSubscription(), {
      wrapper: createWrapper(queryClient),
    });

    act(() => realtime.handler?.({ conversationId, type: 2, name: 'Updated product team' }));

    expect(realtime.subscribe).toHaveBeenCalledWith(
      'GroupConversationChanged',
      expect.any(Function),
    );
    expect(invalidateQueries).toHaveBeenCalledWith({ queryKey: workspaceQueryKey, exact: true });
    expect(invalidateQueries).toHaveBeenCalledWith({
      queryKey: getGroupsQueryOptions().queryKey,
    });
    expect(invalidateQueries).not.toHaveBeenCalledWith({
      queryKey: otherWorkspaceQueryKey,
      exact: true,
    });

    unmount();
    expect(realtime.unsubscribe).toHaveBeenCalledOnce();
  });

  it('ignores a malformed event', () => {
    const queryClient = createTestQueryClient();
    const invalidateQueries = vi.spyOn(queryClient, 'invalidateQueries');
    renderHook(() => useGroupConversationChangedSubscription(), {
      wrapper: createWrapper(queryClient),
    });

    act(() => realtime.handler?.({ conversationId, type: 1, name: 'Invalid' }));

    expect(invalidateQueries).not.toHaveBeenCalled();
  });
});
