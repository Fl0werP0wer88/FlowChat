import { useQueryClient, type QueryKey } from '@tanstack/react-query';
import { useEffect, useRef } from 'react';

import { subscribeToRealtimeEvent } from '@/lib/realtime/realtime-client';

import type { ConversationMessageBufferState } from '../../cache/conversation-message-buffer';

import { applyMessageReceived } from './apply-message-received';
import { parseMessageReceived } from './message-received-event';

export interface UseMessageReceivedSubscriptionOptions {
  activeConversationId: string | null;
  queryKey: QueryKey;
  enabled: boolean;
  synchronizeMessages: () => Promise<number | null>;
}

export function useMessageReceivedSubscription<
  TState extends ConversationMessageBufferState = ConversationMessageBufferState,
>(options: UseMessageReceivedSubscriptionOptions) {
  const queryClient = useQueryClient();
  const optionsRef = useRef(options);

  useEffect(() => {
    optionsRef.current = options;
  }, [options]);

  useEffect(() => {
    const handleMessageReceived = (payload: unknown) => {
      const message = parseMessageReceived(payload);
      const { activeConversationId, queryKey, enabled, synchronizeMessages } = optionsRef.current;
      if (!enabled || !message || activeConversationId !== message.conversationId) return;

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
        void synchronizeMessages();
      }
    };

    return subscribeToRealtimeEvent('MessageReceived', handleMessageReceived);
  }, [queryClient]);
}
