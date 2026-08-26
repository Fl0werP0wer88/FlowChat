import { useQueryClient, type QueryKey } from '@tanstack/react-query';
import { useCallback, useEffect, useRef } from 'react';

import type { ConversationMessageBufferState } from '../cache/conversation-message-buffer';
import { useConversationMessageSync } from '../cache/use-conversation-message-sync';

import { applyMessageReceived } from './message-received/apply-message-received';
import { useConversationWorkspaceSubscription } from './use-conversation-workspace-subscription';
import type { UseConversationWorkspaceSubscriptionOptions } from './use-conversation-workspace-subscription';

type ConversationMetadataCallbacks = Pick<
  UseConversationWorkspaceSubscriptionOptions,
  'onGroupConversationChanged' | 'onParticipantsAdded' | 'onParticipantsRemoved'
>;

export interface UseConversationWorkspaceRealtimeSyncOptions extends ConversationMetadataCallbacks {
  conversationId: string | null;
  queryKey: QueryKey;
  enabled: boolean;
}

export function useConversationWorkspaceRealtimeSync<
  TState extends ConversationMessageBufferState = ConversationMessageBufferState,
>({
  conversationId,
  queryKey,
  enabled,
  onGroupConversationChanged,
  onParticipantsAdded,
  onParticipantsRemoved,
}: UseConversationWorkspaceRealtimeSyncOptions) {
  const queryClient = useQueryClient();
  const queryKeyRef = useRef(queryKey);
  const synchronizeMessages = useConversationMessageSync<TState>({
    conversationId,
    queryKey,
    enabled,
  });

  useEffect(() => {
    queryKeyRef.current = queryKey;
  }, [queryKey]);

  const handleMessageReceived = useCallback(
    (
      message: Parameters<
        NonNullable<UseConversationWorkspaceSubscriptionOptions['onMessageReceived']>
      >[0],
    ) => {
      if (!enabled) return;

      let needsCatchUp = false;
      let requiresRefetch = false;
      queryClient.setQueryData<TState>(queryKeyRef.current, (current) => {
        const result = applyMessageReceived(current, message);
        if (!result) return current;

        needsCatchUp = result.needsCatchUp;
        requiresRefetch = result.requiresRefetch;
        return result.state;
      });

      if (requiresRefetch) {
        void queryClient.invalidateQueries({
          queryKey: queryKeyRef.current,
          exact: true,
        });
      } else if (needsCatchUp) {
        void synchronizeMessages();
      }
    },
    [enabled, queryClient, synchronizeMessages],
  );

  useConversationWorkspaceSubscription({
    activeConversationId: enabled ? conversationId : null,
    onMessageReceived: handleMessageReceived,
    onGroupConversationChanged,
    onParticipantsAdded,
    onParticipantsRemoved,
    onReconnected: () => {
      void synchronizeMessages();
    },
  });

  return synchronizeMessages;
}
