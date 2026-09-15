import { act, render, screen } from '@testing-library/react';

import { useAuthStore } from '@/stores/auth-store';

import { RealtimeBootstrap } from '../realtime-bootstrap';

const realtime = vi.hoisted(() => ({
  start: vi.fn(() => Promise.resolve()),
  stop: vi.fn(),
  presenceSubscription: vi.fn(),
  messageSubscription: vi.fn(),
  groupSubscription: vi.fn(),
  participantsAddedSubscription: vi.fn(),
  participantsRemovedSubscription: vi.fn(),
}));

vi.mock('@/lib/realtime/realtime-client', () => ({
  startRealtimeConnection: realtime.start,
  stopRealtimeConnection: realtime.stop,
}));

vi.mock(
  '@/features/chat/conversations/duet/realtime/presence-changed/use-duet-presence-subscription',
  () => ({
    useDuetPresenceSubscription: realtime.presenceSubscription,
  }),
);
vi.mock(
  '@/features/chat/conversation-workspace/realtime/message-received/use-message-received-subscription',
  () => ({ useMessageReceivedSubscription: realtime.messageSubscription }),
);
vi.mock(
  '@/features/chat/conversation-workspace/realtime/group-conversation-changed/use-group-conversation-changed-subscription',
  () => ({ useGroupConversationChangedSubscription: realtime.groupSubscription }),
);
vi.mock(
  '@/features/chat/conversation-workspace/realtime/conversation-participants-added/use-conversation-participants-added-subscription',
  () => ({
    useConversationParticipantsAddedSubscription: realtime.participantsAddedSubscription,
  }),
);
vi.mock(
  '@/features/chat/conversation-workspace/realtime/conversation-participants-removed/use-conversation-participants-removed-subscription',
  () => ({
    useConversationParticipantsRemovedSubscription: realtime.participantsRemovedSubscription,
  }),
);

describe('RealtimeBootstrap', () => {
  beforeEach(() => {
    realtime.start.mockClear();
    realtime.stop.mockClear();
    realtime.presenceSubscription.mockClear();
    realtime.messageSubscription.mockClear();
    realtime.groupSubscription.mockClear();
    realtime.participantsAddedSubscription.mockClear();
    realtime.participantsRemovedSubscription.mockClear();
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
    expect(realtime.presenceSubscription).toHaveBeenCalledOnce();
    expect(realtime.messageSubscription).toHaveBeenCalledOnce();
    expect(realtime.groupSubscription).toHaveBeenCalledOnce();
    expect(realtime.participantsAddedSubscription).toHaveBeenCalledOnce();
    expect(realtime.participantsRemovedSubscription).toHaveBeenCalledOnce();
    expect(realtime.start).toHaveBeenCalledOnce();

    act(() => useAuthStore.getState().clearSession());

    expect(realtime.stop).toHaveBeenCalledOnce();
  });
});
