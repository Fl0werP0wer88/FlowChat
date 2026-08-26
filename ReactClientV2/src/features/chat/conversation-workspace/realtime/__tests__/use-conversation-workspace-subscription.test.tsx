import { renderHook } from '@testing-library/react';

import {
  useConversationWorkspaceSubscription,
  type UseConversationWorkspaceSubscriptionOptions,
} from '../use-conversation-workspace-subscription';

const subscriptions = vi.hoisted(() => ({
  synchronizeMessages: vi.fn(() => Promise.resolve<number | null>(null)),
  messageReceived: vi.fn(),
  groupConversationChanged: vi.fn(),
  participantsAdded: vi.fn(),
  participantsRemoved: vi.fn(),
}));

vi.mock('../message-received/use-message-received-subscription', () => ({
  useMessageReceivedSubscription: subscriptions.messageReceived.mockImplementation(
    () => subscriptions.synchronizeMessages,
  ),
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
describe('useConversationWorkspaceSubscription', () => {
  beforeEach(() => {
    for (const subscription of Object.values(subscriptions)) subscription.mockClear();
  });

  it('composes workspace subscriptions and returns message synchronization', () => {
    const options: UseConversationWorkspaceSubscriptionOptions = {
      conversationId: '40c3cd3b-69d8-4af3-b1a7-f9174537fb97',
      queryKey: ['conversation-workspace'],
      enabled: true,
      onGroupConversationChanged: vi.fn(),
      onParticipantsAdded: vi.fn(),
      onParticipantsRemoved: vi.fn(),
    };

    const { result } = renderHook(() => useConversationWorkspaceSubscription(options));

    expect(result.current).toBe(subscriptions.synchronizeMessages);
    expect(subscriptions.messageReceived).toHaveBeenCalledWith({
      conversationId: options.conversationId,
      queryKey: options.queryKey,
      enabled: options.enabled,
    });
    expect(subscriptions.groupConversationChanged).toHaveBeenCalledWith({
      activeConversationId: options.conversationId,
      onGroupConversationChanged: options.onGroupConversationChanged,
    });
    expect(subscriptions.participantsAdded).toHaveBeenCalledWith({
      activeConversationId: options.conversationId,
      onParticipantsAdded: options.onParticipantsAdded,
    });
    expect(subscriptions.participantsRemoved).toHaveBeenCalledWith({
      activeConversationId: options.conversationId,
      onParticipantsRemoved: options.onParticipantsRemoved,
    });
  });

  it('disables active-conversation subscriptions when synchronization is disabled', () => {
    renderHook(() =>
      useConversationWorkspaceSubscription({
        conversationId: '40c3cd3b-69d8-4af3-b1a7-f9174537fb97',
        queryKey: ['conversation-workspace'],
        enabled: false,
      }),
    );

    expect(subscriptions.groupConversationChanged).toHaveBeenCalledWith({
      activeConversationId: null,
      onGroupConversationChanged: undefined,
    });
  });
});
