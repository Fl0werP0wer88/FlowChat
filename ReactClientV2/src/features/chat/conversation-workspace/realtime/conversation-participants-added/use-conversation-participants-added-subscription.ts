import { useEffect, useRef } from 'react';

import { subscribeToRealtimeEvent } from '@/lib/realtime/realtime-client';

import {
  parseConversationParticipantsAdded,
  type ConversationParticipantsAddedEvent,
} from './conversation-participants-added-event';

export interface UseConversationParticipantsAddedSubscriptionOptions {
  activeConversationId: string | null;
  onParticipantsAdded?: (event: ConversationParticipantsAddedEvent) => void;
}

export function useConversationParticipantsAddedSubscription(
  options: UseConversationParticipantsAddedSubscriptionOptions,
) {
  const optionsRef = useRef(options);

  useEffect(() => {
    optionsRef.current = options;
  }, [options]);

  useEffect(() => {
    const handleParticipantsAdded = (payload: unknown) => {
      const event = parseConversationParticipantsAdded(payload);
      if (!event || optionsRef.current.activeConversationId !== event.conversationId) return;

      optionsRef.current.onParticipantsAdded?.(event);
    };

    return subscribeToRealtimeEvent('ConversationParticipantsAdded', handleParticipantsAdded);
  }, []);
}
