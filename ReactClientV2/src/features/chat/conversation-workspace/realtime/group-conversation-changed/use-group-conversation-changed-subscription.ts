import { useEffect, useRef } from 'react';

import { subscribeToRealtimeEvent } from '@/lib/realtime/realtime-client';

import {
  parseGroupConversationChanged,
  type GroupConversationChangedEvent,
} from './group-conversation-changed-event';

export interface UseGroupConversationChangedSubscriptionOptions {
  activeConversationId: string | null;
  onGroupConversationChanged?: (event: GroupConversationChangedEvent) => void;
}

export function useGroupConversationChangedSubscription(
  options: UseGroupConversationChangedSubscriptionOptions,
) {
  const optionsRef = useRef(options);

  useEffect(() => {
    optionsRef.current = options;
  }, [options]);

  useEffect(() => {
    const handleGroupConversationChanged = (payload: unknown) => {
      const event = parseGroupConversationChanged(payload);
      if (!event || optionsRef.current.activeConversationId !== event.conversationId) return;

      optionsRef.current.onGroupConversationChanged?.(event);
    };

    return subscribeToRealtimeEvent('GroupConversationChanged', handleGroupConversationChanged);
  }, []);
}
