import { useQueryClient } from '@tanstack/react-query';
import { useEffect } from 'react';

import { getDuetsWithPresenceQueryOptions } from '@/features/chat/conversations/duet/api/get-duets-with-presence';
import { getGroupsQueryOptions } from '@/features/chat/conversations/group/api/get-groups';
import { subscribeToRealtimeEvent } from '@/lib/realtime/realtime-client';

import { invalidateConversationWorkspaceQueries } from '../../cache/conversation-workspace-query-cache';

import { parseConversationParticipantsRemoved } from './conversation-participants-removed-event';

export function useConversationParticipantsRemovedSubscription() {
  const queryClient = useQueryClient();

  useEffect(() => {
    const handleParticipantsRemoved = (payload: unknown) => {
      const event = parseConversationParticipantsRemoved(payload);
      if (!event) return;

      const conversationListQueryKey =
        event.conversationType === 1
          ? getDuetsWithPresenceQueryOptions().queryKey
          : getGroupsQueryOptions().queryKey;
      void invalidateConversationWorkspaceQueries(queryClient, event.conversationId);
      void queryClient.invalidateQueries({ queryKey: conversationListQueryKey });
    };

    return subscribeToRealtimeEvent('ConversationParticipantsRemoved', handleParticipantsRemoved);
  }, [queryClient]);
}
