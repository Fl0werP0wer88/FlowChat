import { useQueryClient } from '@tanstack/react-query';
import { useEffect } from 'react';

import { getDuetsWithPresenceQueryOptions } from '@/features/chat/conversations/duet/api/get-duets-with-presence';
import { getGroupsQueryOptions } from '@/features/chat/conversations/group/api/get-groups';
import { subscribeToRealtimeEvent } from '@/lib/realtime/realtime-client';

import { invalidateConversationWorkspaceQueries } from '../../cache/conversation-workspace-query-cache';

import { parseConversationParticipantsAdded } from './conversation-participants-added-event';

export function useConversationParticipantsAddedSubscription() {
  const queryClient = useQueryClient();

  useEffect(() => {
    const handleParticipantsAdded = (payload: unknown) => {
      const event = parseConversationParticipantsAdded(payload);
      if (!event) return;

      const conversationListQueryKey =
        event.conversationType === 1
          ? getDuetsWithPresenceQueryOptions().queryKey
          : getGroupsQueryOptions().queryKey;
      void invalidateConversationWorkspaceQueries(queryClient, event.conversationId);
      void queryClient.invalidateQueries({ queryKey: conversationListQueryKey });
    };

    return subscribeToRealtimeEvent('ConversationParticipantsAdded', handleParticipantsAdded);
  }, [queryClient]);
}
