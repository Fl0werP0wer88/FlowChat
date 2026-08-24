import { useEffect, useRef } from 'react';

import {
  subscribeToRealtimeEvent,
  subscribeToRealtimeReconnected,
} from '@/lib/realtime/realtime-client';

import type { ConversationMessage } from '../api/conversation-contracts';

import {
  parseConversationParticipantsAdded,
  parseConversationParticipantsRemoved,
  type ConversationParticipantsChangedEvent,
} from './conversation-participants-changed';
import {
  parseGroupConversationChanged,
  type GroupConversationChangedEvent,
} from './group-conversation-changed';
import { parseMessageReceived } from './message-received';

export interface UseConversationWorkspaceSubscriptionOptions {
  activeConversationId: string | null;
  onMessageReceived?: (message: ConversationMessage) => void;
  onGroupConversationChanged?: (event: GroupConversationChangedEvent) => void;
  onParticipantsAdded?: (event: ConversationParticipantsChangedEvent) => void;
  onParticipantsRemoved?: (event: ConversationParticipantsChangedEvent) => void;
  onReconnected?: (conversationId: string) => void;
}

export function useConversationWorkspaceSubscription(
  options: UseConversationWorkspaceSubscriptionOptions,
) {
  const optionsRef = useRef(options);

  useEffect(() => {
    optionsRef.current = options;
  }, [options]);

  useEffect(() => {
    const isActiveConversation = (conversationId: string) =>
      optionsRef.current.activeConversationId === conversationId;

    const handleMessageReceived = (payload: unknown) => {
      const message = parseMessageReceived(payload);
      if (!message || !isActiveConversation(message.conversationId)) return;

      optionsRef.current.onMessageReceived?.(message);
    };
    const handleGroupConversationChanged = (payload: unknown) => {
      const event = parseGroupConversationChanged(payload);
      if (!event || !isActiveConversation(event.conversationId)) return;

      optionsRef.current.onGroupConversationChanged?.(event);
    };
    const handleParticipantsAdded = (payload: unknown) => {
      const event = parseConversationParticipantsAdded(payload);
      if (!event || !isActiveConversation(event.conversationId)) return;

      optionsRef.current.onParticipantsAdded?.(event);
    };
    const handleParticipantsRemoved = (payload: unknown) => {
      const event = parseConversationParticipantsRemoved(payload);
      if (!event || !isActiveConversation(event.conversationId)) return;

      optionsRef.current.onParticipantsRemoved?.(event);
    };
    const handleReconnected = () => {
      const { activeConversationId, onReconnected } = optionsRef.current;
      if (activeConversationId) onReconnected?.(activeConversationId);
    };

    const unsubscribeMessage = subscribeToRealtimeEvent('MessageReceived', handleMessageReceived);
    const unsubscribeGroup = subscribeToRealtimeEvent(
      'GroupConversationChanged',
      handleGroupConversationChanged,
    );
    const unsubscribeParticipantsAdded = subscribeToRealtimeEvent(
      'ConversationParticipantsAdded',
      handleParticipantsAdded,
    );
    const unsubscribeParticipantsRemoved = subscribeToRealtimeEvent(
      'ConversationParticipantsRemoved',
      handleParticipantsRemoved,
    );
    const unsubscribeReconnected = subscribeToRealtimeReconnected(handleReconnected);

    return () => {
      unsubscribeMessage();
      unsubscribeGroup();
      unsubscribeParticipantsAdded();
      unsubscribeParticipantsRemoved();
      unsubscribeReconnected();
    };
  }, []);
}
