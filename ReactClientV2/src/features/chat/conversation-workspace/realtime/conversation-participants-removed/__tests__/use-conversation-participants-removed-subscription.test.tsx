import { QueryClientProvider } from '@tanstack/react-query';
import { act, renderHook } from '@testing-library/react';
import type { ReactNode } from 'react';

import { getDuetsWithPresenceQueryOptions } from '@/features/chat/conversations/duet/api/get-duets-with-presence';
import { getGroupsQueryOptions } from '@/features/chat/conversations/group/api/get-groups';
import { createTestQueryClient } from '@/testing/test-utils';

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
const participantUserId = '14c11faa-8bd7-4608-abcf-26985f3f62be';
const workspaceQueryKey = ['conversation-workspace', 'group', conversationId] as const;

function createWrapper(queryClient: ReturnType<typeof createTestQueryClient>) {
  return function Wrapper({ children }: { children: ReactNode }) {
    return <QueryClientProvider client={queryClient}>{children}</QueryClientProvider>;
  };
}

describe('useConversationParticipantsRemovedSubscription', () => {
  beforeEach(() => {
    realtime.handler = undefined;
    realtime.subscribe.mockClear();
    realtime.unsubscribe.mockClear();
  });

  it.each([
    [1, getDuetsWithPresenceQueryOptions().queryKey],
    [2, getGroupsQueryOptions().queryKey],
  ] as const)(
    'invalidates workspace and conversation list for type %s',
    (conversationType, listKey) => {
      const queryClient = createTestQueryClient();
      queryClient.setQueryData(workspaceQueryKey, { conversationId });
      const invalidateQueries = vi.spyOn(queryClient, 'invalidateQueries');
      const { unmount } = renderHook(() => useConversationParticipantsRemovedSubscription(), {
        wrapper: createWrapper(queryClient),
      });

      act(() =>
        realtime.handler?.({
          conversationId,
          conversationType,
          participantUserIds: [participantUserId],
        }),
      );

      expect(realtime.subscribe).toHaveBeenCalledWith(
        'ConversationParticipantsRemoved',
        expect.any(Function),
      );
      expect(invalidateQueries).toHaveBeenCalledWith({ queryKey: workspaceQueryKey, exact: true });
      expect(invalidateQueries).toHaveBeenCalledWith({ queryKey: listKey });
      unmount();
      expect(realtime.unsubscribe).toHaveBeenCalledOnce();
    },
  );

  it('ignores a malformed event', () => {
    const queryClient = createTestQueryClient();
    const invalidateQueries = vi.spyOn(queryClient, 'invalidateQueries');
    renderHook(() => useConversationParticipantsRemovedSubscription(), {
      wrapper: createWrapper(queryClient),
    });

    act(() =>
      realtime.handler?.({
        conversationId,
        conversationType: 2,
        participantUserIds: ['invalid'],
      }),
    );

    expect(invalidateQueries).not.toHaveBeenCalled();
  });
});
