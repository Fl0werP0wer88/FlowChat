import { act, render, screen } from '@testing-library/react';

import { useAuthStore } from '@/stores/auth-store';

import { RealtimeBootstrap } from '../realtime-bootstrap';

const realtime = vi.hoisted(() => ({
  start: vi.fn(() => Promise.resolve()),
  stop: vi.fn(),
  subscribe: vi.fn(),
}));

vi.mock('@/lib/realtime/realtime-client', () => ({
  startRealtimeConnection: realtime.start,
  stopRealtimeConnection: realtime.stop,
}));

vi.mock('@/features/chat/conversations/duet/realtime/use-duet-presence-subscription', () => ({
  useDuetPresenceSubscription: realtime.subscribe,
}));

describe('RealtimeBootstrap', () => {
  beforeEach(() => {
    realtime.start.mockClear();
    realtime.stop.mockClear();
    realtime.subscribe.mockClear();
  });

  it('starts after login and stops after logout', () => {
    useAuthStore.setState({
      session: {
        accessToken: 'access-token',
        expiresAtUtc: '2026-08-18T12:00:00+00:00',
        user: { id: 'user-id', email: null, friendlyUserId: 'alex', roles: [] },
      },
      bootstrapStatus: 'ready',
    });

    render(
      <RealtimeBootstrap>
        <p>Application</p>
      </RealtimeBootstrap>,
    );

    expect(screen.getByText('Application')).toBeInTheDocument();
    expect(realtime.subscribe).toHaveBeenCalledOnce();
    expect(realtime.start).toHaveBeenCalledOnce();

    act(() => useAuthStore.getState().clearSession());

    expect(realtime.stop).toHaveBeenCalledOnce();
  });
});
