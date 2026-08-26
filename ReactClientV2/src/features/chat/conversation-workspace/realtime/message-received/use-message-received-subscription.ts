import { useEffect, useRef } from 'react';

import { subscribeToRealtimeEvent } from '@/lib/realtime/realtime-client';

import type { ConversationMessage } from '../../api/conversation-contracts';

import { parseMessageReceived } from './message-received-event';

export interface UseMessageReceivedSubscriptionOptions {
  activeConversationId: string | null;
  onMessageReceived?: (message: ConversationMessage) => void;
}

export function useMessageReceivedSubscription(options: UseMessageReceivedSubscriptionOptions) {
  const optionsRef = useRef(options);

  useEffect(() => {
    optionsRef.current = options;
  }, [options]);

  useEffect(() => {
    const handleMessageReceived = (payload: unknown) => {
      const message = parseMessageReceived(payload);
      if (!message || optionsRef.current.activeConversationId !== message.conversationId) return;

      optionsRef.current.onMessageReceived?.(message);
    };

    return subscribeToRealtimeEvent('MessageReceived', handleMessageReceived);
  }, []);
}
