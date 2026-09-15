import { render } from '@testing-library/react';

import { useAuthStore } from '@/stores/auth-store';
import { createAccessToken } from '@/testing/token-fixture';

import { useSessionRefresher } from '../use-session-refresher';

const { refreshSessionMock } = vi.hoisted(() => ({
  refreshSessionMock: vi.fn(() => Promise.resolve(null)),
}));

vi.mock('@/lib/auth-session', () => ({
  refreshSession: () => refreshSessionMock(),
}));

function SessionRefresherHarness() {
  useSessionRefresher();
  return null;
}

describe('useSessionRefresher', () => {
  it('refreshes one minute before the access token expires', async () => {
    vi.useFakeTimers();
    const now = new Date('2026-08-16T10:00:00.000Z');
    vi.setSystemTime(now);
    useAuthStore.getState().setSession({
      accessToken: createAccessToken(),
      expiresAtUtc: new Date(now.getTime() + 61_000).toISOString(),
      user: {
        id: '82b0c1ca-d57a-43b0-a871-39a97056af89',
        email: 'alex@example.com',
        friendlyUserId: 'alex.morgan',
        roles: ['User'],
      },
    });

    render(<SessionRefresherHarness />);
    await vi.advanceTimersByTimeAsync(999);
    expect(refreshSessionMock).not.toHaveBeenCalled();
    await vi.advanceTimersByTimeAsync(1);
    expect(refreshSessionMock).toHaveBeenCalledOnce();

    vi.useRealTimers();
  });
});
