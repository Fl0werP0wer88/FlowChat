import { act, renderHook } from '@testing-library/react';

import {
  useConversationWorkspaceSubscription,
  type UseConversationWorkspaceSubscriptionOptions,
} from '../use-conversation-workspace-subscription';

type EventHandler = (payload: unknown) => void;

const realtime = vi.hoisted(() => ({
  subscriptions: [] as Array<{
    eventName: string;
    handler: EventHandler;
    unsubscribe: ReturnType<typeof vi.fn>;
  }>,
  reconnectedHandler: undefined as (() => void) | undefined,
  unsubscribeReconnected: vi.fn(),
  subscribe: vi.fn(),
  subscribeToReconnected: vi.fn(),
}));

vi.mock('@/lib/realtime/realtime-client', () => ({
  subscribeToRealtimeEvent: realtime.subscribe.mockImplementation(
    (eventName: string, handler: EventHandler) => {
      const unsubscribe = vi.fn();
      realtime.subscriptions.push({ eventName, handler, unsubscribe });
      return unsubscribe;
    },
  ),
  subscribeToRealtimeReconnected: realtime.subscribeToReconnected.mockImplementation(
    (handler: () => void) => {
      realtime.reconnectedHandler = handler;
      return realtime.unsubscribeReconnected;
    },
  ),
}));

const conversationId = '40c3cd3b-69d8-4af3-b1a7-f9174537fb97';
const otherConversationId = '3965a011-e9ec-4379-9b0b-e2d3132f67d8';
const messageId = 'c7d063d8-0db1-4adf-a425-01f9000b8ace';
const senderUserId = '91f65d44-d175-45af-8839-d2d36e7d61f9';
const participantUserId = '14c11faa-8bd7-4608-abcf-26985f3f62be';

function getEventHandler(eventName: string) {
  const subscription = realtime.subscriptions.find((item) => item.eventName === eventName);
  if (!subscription) throw new Error(`Missing ${eventName} subscription.`);
  return subscription.handler;
}

function createMessagePayload(targetConversationId = conversationId) {
  return {
    messageId,
    conversationId: targetConversationId,
    senderUserId,
    text: 'Hello',
    sequenceNum: 7,
    sentAtUtc: '2026-08-24T10:00:00+00:00',
    deliveredAtUtc: '2026-08-24T10:00:01+00:00',
  };
}

describe('useConversationWorkspaceSubscription', () => {
  beforeEach(() => {
    realtime.subscriptions.length = 0;
    realtime.reconnectedHandler = undefined;
    realtime.unsubscribeReconnected.mockClear();
    realtime.subscribe.mockClear();
    realtime.subscribeToReconnected.mockClear();
  });

  it('routes valid events for the active conversation and unsubscribes on unmount', () => {
    const onMessageReceived = vi.fn();
    const onGroupConversationChanged = vi.fn();
    const onParticipantsAdded = vi.fn();
    const onParticipantsRemoved = vi.fn();
    const onReconnected = vi.fn();
    const { unmount } = renderHook(() =>
      useConversationWorkspaceSubscription({
        activeConversationId: conversationId,
        onMessageReceived,
        onGroupConversationChanged,
        onParticipantsAdded,
        onParticipantsRemoved,
        onReconnected,
      }),
    );

    expect(realtime.subscribe.mock.calls.map(([eventName]) => eventName)).toEqual([
      'MessageReceived',
      'GroupConversationChanged',
      'ConversationParticipantsAdded',
      'ConversationParticipantsRemoved',
    ]);
    expect(realtime.subscribeToReconnected).toHaveBeenCalledOnce();

    act(() => {
      getEventHandler('MessageReceived')(createMessagePayload());
      getEventHandler('GroupConversationChanged')({
        conversationId,
        type: 2,
        name: 'Product team',
      });
      getEventHandler('ConversationParticipantsAdded')({
        conversationId,
        conversationType: 2,
        participantUserIds: [participantUserId],
      });
      getEventHandler('ConversationParticipantsRemoved')({
        conversationId,
        conversationType: 2,
        participantUserIds: [participantUserId],
      });
      realtime.reconnectedHandler?.();
    });

    expect(onMessageReceived).toHaveBeenCalledWith({
      id: messageId,
      conversationId,
      senderUserId,
      text: 'Hello',
      sequenceNum: 7,
      sentAtUtc: '2026-08-24T10:00:00+00:00',
    });
    expect(onGroupConversationChanged).toHaveBeenCalledWith({
      conversationId,
      type: 2,
      name: 'Product team',
    });
    expect(onParticipantsAdded).toHaveBeenCalledOnce();
    expect(onParticipantsRemoved).toHaveBeenCalledOnce();
    expect(onReconnected).toHaveBeenCalledWith(conversationId);

    unmount();
    for (const subscription of realtime.subscriptions) {
      expect(subscription.unsubscribe).toHaveBeenCalledOnce();
    }
    expect(realtime.unsubscribeReconnected).toHaveBeenCalledOnce();
  });

  it('ignores malformed events and events for another conversation', () => {
    const callbacks = {
      onMessageReceived: vi.fn(),
      onGroupConversationChanged: vi.fn(),
      onParticipantsAdded: vi.fn(),
      onParticipantsRemoved: vi.fn(),
    };
    renderHook(() =>
      useConversationWorkspaceSubscription({
        activeConversationId: conversationId,
        ...callbacks,
      }),
    );

    act(() => {
      getEventHandler('MessageReceived')(createMessagePayload(otherConversationId));
      getEventHandler('MessageReceived')({ ...createMessagePayload(), sequenceNum: -1 });
      getEventHandler('GroupConversationChanged')({
        conversationId: otherConversationId,
        type: 2,
        name: null,
      });
      getEventHandler('ConversationParticipantsAdded')({
        conversationId,
        conversationType: 3,
        participantUserIds: [participantUserId],
      });
      getEventHandler('ConversationParticipantsRemoved')({
        conversationId: otherConversationId,
        conversationType: 2,
        participantUserIds: [participantUserId],
      });
    });

    for (const callback of Object.values(callbacks)) expect(callback).not.toHaveBeenCalled();
  });

  it('uses the latest options without registering subscriptions again', () => {
    const firstMessageHandler = vi.fn();
    const nextMessageHandler = vi.fn();
    const onReconnected = vi.fn();
    const initialProps: UseConversationWorkspaceSubscriptionOptions = {
      activeConversationId: conversationId,
      onMessageReceived: firstMessageHandler,
      onReconnected,
    };
    const { rerender } = renderHook(
      (options: UseConversationWorkspaceSubscriptionOptions) =>
        useConversationWorkspaceSubscription(options),
      { initialProps },
    );

    rerender({
      activeConversationId: otherConversationId,
      onMessageReceived: nextMessageHandler,
      onReconnected,
    });

    act(() => {
      getEventHandler('MessageReceived')(createMessagePayload(otherConversationId));
      realtime.reconnectedHandler?.();
    });

    expect(realtime.subscribe).toHaveBeenCalledTimes(4);
    expect(realtime.subscribeToReconnected).toHaveBeenCalledOnce();
    expect(firstMessageHandler).not.toHaveBeenCalled();
    expect(nextMessageHandler).toHaveBeenCalledOnce();
    expect(onReconnected).toHaveBeenCalledWith(otherConversationId);

    rerender({ activeConversationId: null, onReconnected });
    act(() => realtime.reconnectedHandler?.());
    expect(onReconnected).toHaveBeenCalledOnce();
  });
});
