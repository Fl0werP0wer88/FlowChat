import { QueryClientProvider } from '@tanstack/react-query';
import { act, renderHook } from '@testing-library/react';
import type { ReactNode } from 'react';

import { createTestQueryClient } from '@/testing/test-utils';

import { getDuetsWithPresenceQueryOptions } from '../../../api/get-duets-with-presence';
import { useDuetPresenceSubscription } from '../use-duet-presence-subscription';

const realtime = vi.hoisted(() => ({
  eventHandler: undefined as ((payload: unknown) => void) | undefined,
  reconnectedHandler: undefined as (() => void) | undefined,
  unsubscribeEvent: vi.fn(),
  unsubscribeReconnected: vi.fn(),
}));

vi.mock('@/lib/realtime/realtime-client', () => ({
  subscribeToRealtimeEvent: vi.fn((_eventName: string, handler: (payload: unknown) => void) => {
    realtime.eventHandler = handler;
    return realtime.unsubscribeEvent;
  }),
  subscribeToRealtimeReconnected: vi.fn((handler: () => void) => {
    realtime.reconnectedHandler = handler;
    return realtime.unsubscribeReconnected;
  }),
}));

describe('useDuetPresenceSubscription', () => {
  beforeEach(() => {
    realtime.eventHandler = undefined;
    realtime.reconnectedHandler = undefined;
    realtime.unsubscribeEvent.mockClear();
    realtime.unsubscribeReconnected.mockClear();
  });

  it('updates cached presence and invalidates the duet query after reconnect', async () => {
    const queryClient = createTestQueryClient();
    const queryKey = getDuetsWithPresenceQueryOptions().queryKey;
    queryClient.setQueryData(queryKey, {
      conversations: [
        {
          partnerUserId: '14c11faa-8bd7-4608-abcf-26985f3f62be',
          displayName: null,
          avatarUrl: null,
          email: null,
          isBlocked: false,
          isBlockedByPartner: false,
          isMuted: false,
          isHidden: false,
          conversationId: '40c3cd3b-69d8-4af3-b1a7-f9174537fb97',
          lastReadMsgSeqNum: 0,
          currentMsgSeqNum: 0,
          unreadCount: 0,
          status: 'Active' as const,
          presenceChangedAtUtc: '2026-08-18T10:00:00+00:00',
        },
      ],
    });
    const invalidateQueries = vi.spyOn(queryClient, 'invalidateQueries');

    function Wrapper({ children }: { children: ReactNode }) {
      return <QueryClientProvider client={queryClient}>{children}</QueryClientProvider>;
    }

    const { unmount } = renderHook(() => useDuetPresenceSubscription(), { wrapper: Wrapper });

    act(() => {
      realtime.eventHandler?.({
        userId: '14c11faa-8bd7-4608-abcf-26985f3f62be',
        status: 'Busy',
        changedAtUtc: '2026-08-18T10:01:00+00:00',
      });
    });
    expect(
      queryClient.getQueryData<{ conversations: Array<{ status: string }> }>(queryKey),
    ).toMatchObject({ conversations: [{ status: 'Busy' }] });

    act(() => realtime.reconnectedHandler?.());
    expect(invalidateQueries).toHaveBeenCalledWith({ queryKey });

    unmount();
    expect(realtime.unsubscribeEvent).toHaveBeenCalledOnce();
    expect(realtime.unsubscribeReconnected).toHaveBeenCalledOnce();
  });
});
