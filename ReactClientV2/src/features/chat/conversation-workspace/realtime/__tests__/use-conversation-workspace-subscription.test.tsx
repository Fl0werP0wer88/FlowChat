import { renderHook } from '@testing-library/react';

import {
  useConversationWorkspaceSubscription,
  type UseConversationWorkspaceSubscriptionOptions,
} from '../use-conversation-workspace-subscription';

const subscriptions = vi.hoisted(() => ({
  messageReceived: vi.fn(),
  groupConversationChanged: vi.fn(),
  participantsAdded: vi.fn(),
  participantsRemoved: vi.fn(),
  reconnected: vi.fn(),
}));

vi.mock('../message-received/use-message-received-subscription', () => ({
  useMessageReceivedSubscription: subscriptions.messageReceived,
}));
vi.mock('../group-conversation-changed/use-group-conversation-changed-subscription', () => ({
  useGroupConversationChangedSubscription: subscriptions.groupConversationChanged,
}));
vi.mock(
  '../conversation-participants-added/use-conversation-participants-added-subscription',
  () => ({
    useConversationParticipantsAddedSubscription: subscriptions.participantsAdded,
  }),
);
vi.mock(
  '../conversation-participants-removed/use-conversation-participants-removed-subscription',
  () => ({
    useConversationParticipantsRemovedSubscription: subscriptions.participantsRemoved,
  }),
);
vi.mock('../reconnected/use-conversation-workspace-reconnected-subscription', () => ({
  useConversationWorkspaceReconnectedSubscription: subscriptions.reconnected,
}));

describe('useConversationWorkspaceSubscription', () => {
  beforeEach(() => {
    for (const subscription of Object.values(subscriptions)) subscription.mockClear();
  });

  it('composes all workspace subscriptions with their matching options', () => {
    const options: UseConversationWorkspaceSubscriptionOptions = {
      activeConversationId: '40c3cd3b-69d8-4af3-b1a7-f9174537fb97',
      queryKey: ['conversation-workspace'],
      enabled: true,
      synchronizeMessages: vi.fn(() => Promise.resolve<number | null>(null)),
      onGroupConversationChanged: vi.fn(),
      onParticipantsAdded: vi.fn(),
      onParticipantsRemoved: vi.fn(),
      onReconnected: vi.fn(),
    };

    renderHook(() => useConversationWorkspaceSubscription(options));

    expect(subscriptions.messageReceived).toHaveBeenCalledWith({
      activeConversationId: options.activeConversationId,
      queryKey: options.queryKey,
      enabled: options.enabled,
      synchronizeMessages: options.synchronizeMessages,
    });
    expect(subscriptions.groupConversationChanged).toHaveBeenCalledWith({
      activeConversationId: options.activeConversationId,
      onGroupConversationChanged: options.onGroupConversationChanged,
    });
    expect(subscriptions.participantsAdded).toHaveBeenCalledWith({
      activeConversationId: options.activeConversationId,
      onParticipantsAdded: options.onParticipantsAdded,
    });
    expect(subscriptions.participantsRemoved).toHaveBeenCalledWith({
      activeConversationId: options.activeConversationId,
      onParticipantsRemoved: options.onParticipantsRemoved,
    });
    expect(subscriptions.reconnected).toHaveBeenCalledWith({
      activeConversationId: options.activeConversationId,
      onReconnected: options.onReconnected,
    });
  });
});
