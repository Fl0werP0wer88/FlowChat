import type { ConversationMessage } from '../api/conversation-contracts';

import type { ConversationParticipantsAddedEvent } from './conversation-participants-added/conversation-participants-added-event';
import { useConversationParticipantsAddedSubscription } from './conversation-participants-added/use-conversation-participants-added-subscription';
import type { ConversationParticipantsRemovedEvent } from './conversation-participants-removed/conversation-participants-removed-event';
import { useConversationParticipantsRemovedSubscription } from './conversation-participants-removed/use-conversation-participants-removed-subscription';
import type { GroupConversationChangedEvent } from './group-conversation-changed/group-conversation-changed-event';
import { useGroupConversationChangedSubscription } from './group-conversation-changed/use-group-conversation-changed-subscription';
import { useMessageReceivedSubscription } from './message-received/use-message-received-subscription';
import { useConversationWorkspaceReconnectedSubscription } from './reconnected/use-conversation-workspace-reconnected-subscription';

export interface UseConversationWorkspaceSubscriptionOptions {
  activeConversationId: string | null;
  onMessageReceived?: (message: ConversationMessage) => void;
  onGroupConversationChanged?: (event: GroupConversationChangedEvent) => void;
  onParticipantsAdded?: (event: ConversationParticipantsAddedEvent) => void;
  onParticipantsRemoved?: (event: ConversationParticipantsRemovedEvent) => void;
  onReconnected?: (conversationId: string) => void;
}

export function useConversationWorkspaceSubscription({
  activeConversationId,
  onMessageReceived,
  onGroupConversationChanged,
  onParticipantsAdded,
  onParticipantsRemoved,
  onReconnected,
}: UseConversationWorkspaceSubscriptionOptions) {
  useMessageReceivedSubscription({ activeConversationId, onMessageReceived });
  useGroupConversationChangedSubscription({ activeConversationId, onGroupConversationChanged });
  useConversationParticipantsAddedSubscription({ activeConversationId, onParticipantsAdded });
  useConversationParticipantsRemovedSubscription({ activeConversationId, onParticipantsRemoved });
  useConversationWorkspaceReconnectedSubscription({ activeConversationId, onReconnected });
}
