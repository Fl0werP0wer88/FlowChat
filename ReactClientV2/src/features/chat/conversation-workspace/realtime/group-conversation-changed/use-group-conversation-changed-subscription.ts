import { useQueryClient } from '@tanstack/react-query';
import { useEffect } from 'react';

import { getGroupsQueryOptions } from '@/features/chat/conversations/group/api/get-groups';
import { subscribeToRealtimeEvent } from '@/lib/realtime/realtime-client';

import { invalidateConversationWorkspaceQueries } from '../../cache/conversation-workspace-query-cache';

import { parseGroupConversationChanged } from './group-conversation-changed-event';

export function useGroupConversationChangedSubscription() {
  const queryClient = useQueryClient();

  useEffect(() => {
    const handleGroupConversationChanged = (payload: unknown) => {
      const event = parseGroupConversationChanged(payload);
      if (!event) return;

      void invalidateConversationWorkspaceQueries(queryClient, event.conversationId);
      void queryClient.invalidateQueries({ queryKey: getGroupsQueryOptions().queryKey });
    };

    return subscribeToRealtimeEvent('GroupConversationChanged', handleGroupConversationChanged);
  }, [queryClient]);
}
