import type { QueryKey } from '@tanstack/react-query';

import type { ConversationMessageBufferState } from '../cache/conversation-message-buffer';

import type { ConversationParticipantsAddedEvent } from './conversation-participants-added/conversation-participants-added-event';
import { useConversationParticipantsAddedSubscription } from './conversation-participants-added/use-conversation-participants-added-subscription';
import type { ConversationParticipantsRemovedEvent } from './conversation-participants-removed/conversation-participants-removed-event';
import { useConversationParticipantsRemovedSubscription } from './conversation-participants-removed/use-conversation-participants-removed-subscription';
import type { GroupConversationChangedEvent } from './group-conversation-changed/group-conversation-changed-event';
import { useGroupConversationChangedSubscription } from './group-conversation-changed/use-group-conversation-changed-subscription';
import { useMessageReceivedSubscription } from './message-received/use-message-received-subscription';

export interface UseConversationWorkspaceSubscriptionOptions {
  conversationId: string | null;
  queryKey: QueryKey;
  enabled: boolean;
  onGroupConversationChanged?: (event: GroupConversationChangedEvent) => void;
  onParticipantsAdded?: (event: ConversationParticipantsAddedEvent) => void;
  onParticipantsRemoved?: (event: ConversationParticipantsRemovedEvent) => void;
}

export function useConversationWorkspaceSubscription<
  TState extends ConversationMessageBufferState = ConversationMessageBufferState,
>({
  conversationId,
  queryKey,
  enabled,
  onGroupConversationChanged,
  onParticipantsAdded,
  onParticipantsRemoved,
}: UseConversationWorkspaceSubscriptionOptions) {
  const activeConversationId = enabled ? conversationId : null;
  const synchronizeMessages = useMessageReceivedSubscription<TState>({
    conversationId,
    queryKey,
    enabled,
  });
  useGroupConversationChangedSubscription({ activeConversationId, onGroupConversationChanged });
  useConversationParticipantsAddedSubscription({ activeConversationId, onParticipantsAdded });
  useConversationParticipantsRemovedSubscription({ activeConversationId, onParticipantsRemoved });

  return synchronizeMessages;
}
