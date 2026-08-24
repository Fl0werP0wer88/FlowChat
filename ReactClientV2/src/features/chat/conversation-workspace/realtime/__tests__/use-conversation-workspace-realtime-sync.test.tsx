import { QueryClientProvider } from '@tanstack/react-query';
import { act, renderHook } from '@testing-library/react';
import type { ReactNode } from 'react';

import { createTestQueryClient } from '@/testing/test-utils';

import type { ConversationMessage } from '../../api/conversation-contracts';
import {
  createConversationMessageBuffer,
  type ConversationMessageBufferState,
} from '../../cache/conversation-message-buffer';
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
const senderUserId = '91f65d44-d175-45af-8839-d2d36e7d61f9';
const queryKey = ['conversation-workspace', 'group', conversationId] as const;

function createMessage(sequenceNum: number): ConversationMessage {
  return {
    id: `00000000-0000-4000-8000-${sequenceNum.toString().padStart(12, '0')}`,
    conversationId,
    senderUserId,
    text: `Message ${sequenceNum}`,
    sentAtUtc: `2026-08-24T10:00:${sequenceNum.toString().padStart(2, '0')}+00:00`,
    sequenceNum,
  };
}

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

  it('merges a contiguous event and synchronizes only after a sequence gap', () => {
    const queryClient = createTestQueryClient();
    queryClient.setQueryData(
      queryKey,
      createConversationMessageBuffer({
        messages: [createMessage(1), createMessage(2)],
        currentSequenceNum: 2,
      }),
    );
    renderHook(
      () => useConversationWorkspaceRealtimeSync({ conversationId, queryKey, enabled: true }),
      { wrapper: createWrapper(queryClient) },
    );

    act(() => mocks.subscriptionOptions?.onMessageReceived?.(createMessage(3)));
    expect(mocks.synchronizeMessages).not.toHaveBeenCalled();
    expect(
      queryClient.getQueryData<ConversationMessageBufferState>(queryKey)?.lastContiguousSequenceNum,
    ).toBe(3);

    act(() => mocks.subscriptionOptions?.onMessageReceived?.(createMessage(5)));
    expect(mocks.synchronizeMessages).toHaveBeenCalledOnce();
    expect(
      queryClient.getQueryData<ConversationMessageBufferState>(queryKey)?.pendingMessagesBySequence,
    ).toHaveProperty('5');
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
    act(() => mocks.subscriptionOptions?.onMessageReceived?.(createMessage(3)));
    expect(mocks.synchronizeMessages).not.toHaveBeenCalled();
  });
});
