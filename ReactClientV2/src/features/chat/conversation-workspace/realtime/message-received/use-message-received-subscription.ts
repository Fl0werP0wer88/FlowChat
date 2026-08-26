import { useQueryClient, type QueryKey } from '@tanstack/react-query';
import { useEffect, useRef } from 'react';

import {
  subscribeToRealtimeEvent,
  subscribeToRealtimeReconnected,
} from '@/lib/realtime/realtime-client';

import type { ConversationMessageBufferState } from '../../cache/conversation-message-buffer';
import { useConversationMessageSync } from '../../cache/use-conversation-message-sync';

import { applyMessageReceived } from './apply-message-received';
import { parseMessageReceived } from './message-received-event';

export interface UseMessageReceivedSubscriptionOptions {
  conversationId: string | null;
  queryKey: QueryKey;
  enabled: boolean;
}

export function useMessageReceivedSubscription<
  TState extends ConversationMessageBufferState = ConversationMessageBufferState,
>(options: UseMessageReceivedSubscriptionOptions) {
  const queryClient = useQueryClient();
  const synchronizeMessages = useConversationMessageSync<TState>(options);
  const optionsRef = useRef(options);
  const synchronizeMessagesRef = useRef(synchronizeMessages);

  useEffect(() => {
    optionsRef.current = options;
    synchronizeMessagesRef.current = synchronizeMessages;
  }, [options, synchronizeMessages]);

  useEffect(() => {
    const handleMessageReceived = (payload: unknown) => {
      const message = parseMessageReceived(payload);
      const { conversationId, queryKey, enabled } = optionsRef.current;
      if (!enabled || !message || conversationId !== message.conversationId) return;

      let needsCatchUp = false;
      let requiresRefetch = false;
      queryClient.setQueryData<TState>(queryKey, (current) => {
        const result = applyMessageReceived(current, message);
        if (!result) return current;

        needsCatchUp = result.needsCatchUp;
        requiresRefetch = result.requiresRefetch;
        return result.state;
      });

      if (requiresRefetch) {
        void queryClient.invalidateQueries({ queryKey, exact: true });
      } else if (needsCatchUp) {
        void synchronizeMessagesRef.current();
      }
    };

    const unsubscribeMessage = subscribeToRealtimeEvent('MessageReceived', handleMessageReceived);
    const unsubscribeReconnected = subscribeToRealtimeReconnected(() => {
      void synchronizeMessagesRef.current();
    });

    return () => {
      unsubscribeMessage();
      unsubscribeReconnected();
    };
  }, [queryClient]);

  return synchronizeMessages;
}
