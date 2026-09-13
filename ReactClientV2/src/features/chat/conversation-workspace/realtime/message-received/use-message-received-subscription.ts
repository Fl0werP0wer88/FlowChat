import { useQueryClient } from '@tanstack/react-query';
import { useEffect } from 'react';

import {
  subscribeToRealtimeEvent,
  subscribeToRealtimeReconnected,
} from '@/lib/realtime/realtime-client';

import {
  getConversationWorkspaceQueries,
  type ConversationWorkspaceCacheEntry,
} from '../../cache/conversation-workspace-query-cache';
import { useConversationMessageSync } from '../../cache/use-conversation-message-sync';

import { applyMessageReceived } from './apply-message-received';
import { parseMessageReceived } from './message-received-event';

export function useMessageReceivedSubscription() {
  const queryClient = useQueryClient();
  const { synchronizeQuery, synchronizeAll } = useConversationMessageSync();

  useEffect(() => {
    const handleMessageReceived = (payload: unknown) => {
      const message = parseMessageReceived(payload);
      if (!message) return;

      for (const [queryKey] of getConversationWorkspaceQueries(
        queryClient,
        message.conversationId,
      )) {
        let needsCatchUp = false;
        let requiresRefetch = false;
        queryClient.setQueryData<ConversationWorkspaceCacheEntry>(queryKey, (current) => {
          const result = applyMessageReceived(current, message);
          if (!result) return current;

          needsCatchUp = result.needsCatchUp;
          requiresRefetch = result.requiresRefetch;
          return result.state;
        });

        if (requiresRefetch) {
          void queryClient.invalidateQueries({ queryKey, exact: true });
        } else if (needsCatchUp) {
          void synchronizeQuery(message.conversationId, queryKey);
        }
      }
    };

    const unsubscribeMessage = subscribeToRealtimeEvent('MessageReceived', handleMessageReceived);
    const unsubscribeReconnected = subscribeToRealtimeReconnected(() => {
      void synchronizeAll();
    });

    return () => {
      unsubscribeMessage();
      unsubscribeReconnected();
    };
  }, [queryClient, synchronizeAll, synchronizeQuery]);
}
