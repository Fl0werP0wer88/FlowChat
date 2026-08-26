import { renderHook } from '@testing-library/react';

import { useConversationWorkspaceRealtimeSync } from '../use-conversation-workspace-realtime-sync';
import type { UseConversationWorkspaceSubscriptionOptions } from '../use-conversation-workspace-subscription';

const mocks = vi.hoisted(() => ({
  subscriptionOptions: undefined as UseConversationWorkspaceSubscriptionOptions | undefined,
  synchronizeMessages: vi.fn(() => Promise.resolve<number | null>(null)),
}));

vi.mock('../use-conversation-workspace-subscription', () => ({
  useConversationWorkspaceSubscription: (options: UseConversationWorkspaceSubscriptionOptions) => {
    mocks.subscriptionOptions = options;
    return mocks.synchronizeMessages;
  },
}));

const conversationId = '40c3cd3b-69d8-4af3-b1a7-f9174537fb97';
const queryKey = ['conversation-workspace', 'group', conversationId] as const;

describe('useConversationWorkspaceRealtimeSync', () => {
  beforeEach(() => {
    mocks.subscriptionOptions = undefined;
    mocks.synchronizeMessages.mockClear();
  });

  it('returns the message synchronizer and forwards workspace options', () => {
    const onGroupConversationChanged = vi.fn();
    const onParticipantsAdded = vi.fn();
    const onParticipantsRemoved = vi.fn();
    const { result } = renderHook(() =>
      useConversationWorkspaceRealtimeSync({
        conversationId,
        queryKey,
        enabled: true,
        onGroupConversationChanged,
        onParticipantsAdded,
        onParticipantsRemoved,
      }),
    );

    expect(result.current).toBe(mocks.synchronizeMessages);
    expect(mocks.subscriptionOptions).toEqual({
      conversationId,
      queryKey,
      enabled: true,
      onGroupConversationChanged,
      onParticipantsAdded,
      onParticipantsRemoved,
    });
  });

  it('forwards the disabled state without creating synchronization itself', () => {
    renderHook(() =>
      useConversationWorkspaceRealtimeSync({ conversationId, queryKey, enabled: false }),
    );

    expect(mocks.subscriptionOptions).toMatchObject({ conversationId, queryKey, enabled: false });
    expect(mocks.synchronizeMessages).not.toHaveBeenCalled();
  });
});
