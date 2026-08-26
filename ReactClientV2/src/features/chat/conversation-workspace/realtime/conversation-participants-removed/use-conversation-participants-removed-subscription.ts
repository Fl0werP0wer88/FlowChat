import { useEffect, useRef } from 'react';

import { subscribeToRealtimeEvent } from '@/lib/realtime/realtime-client';

import {
  parseConversationParticipantsRemoved,
  type ConversationParticipantsRemovedEvent,
} from './conversation-participants-removed-event';

export interface UseConversationParticipantsRemovedSubscriptionOptions {
  activeConversationId: string | null;
  onParticipantsRemoved?: (event: ConversationParticipantsRemovedEvent) => void;
}

export function useConversationParticipantsRemovedSubscription(
  options: UseConversationParticipantsRemovedSubscriptionOptions,
) {
  const optionsRef = useRef(options);

  useEffect(() => {
    optionsRef.current = options;
  }, [options]);

  useEffect(() => {
    const handleParticipantsRemoved = (payload: unknown) => {
      const event = parseConversationParticipantsRemoved(payload);
      if (!event || optionsRef.current.activeConversationId !== event.conversationId) return;

      optionsRef.current.onParticipantsRemoved?.(event);
    };

    return subscribeToRealtimeEvent('ConversationParticipantsRemoved', handleParticipantsRemoved);
  }, []);
}
