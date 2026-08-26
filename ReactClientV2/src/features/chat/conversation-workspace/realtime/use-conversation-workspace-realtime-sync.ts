import type { QueryKey } from '@tanstack/react-query';

import type { ConversationMessageBufferState } from '../cache/conversation-message-buffer';

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
  return useConversationWorkspaceSubscription<TState>({
    conversationId,
    queryKey,
    enabled,
    onGroupConversationChanged,
    onParticipantsAdded,
    onParticipantsRemoved,
  });
}
