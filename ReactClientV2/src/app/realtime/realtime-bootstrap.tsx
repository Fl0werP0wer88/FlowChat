import { useEffect, type ReactNode } from 'react';

import { useConversationParticipantsAddedSubscription } from '@/features/chat/conversation-workspace/realtime/conversation-participants-added/use-conversation-participants-added-subscription';
import { useConversationParticipantsRemovedSubscription } from '@/features/chat/conversation-workspace/realtime/conversation-participants-removed/use-conversation-participants-removed-subscription';
import { useGroupConversationChangedSubscription } from '@/features/chat/conversation-workspace/realtime/group-conversation-changed/use-group-conversation-changed-subscription';
import { useMessageReceivedSubscription } from '@/features/chat/conversation-workspace/realtime/message-received/use-message-received-subscription';
import { useDuetPresenceSubscription } from '@/features/chat/conversations/duet/realtime/presence-changed/use-duet-presence-subscription';
import { startRealtimeConnection, stopRealtimeConnection } from '@/lib/realtime/realtime-client';
import { useAuthStore } from '@/stores/auth-store';

interface RealtimeBootstrapProps {
  children: ReactNode;
}

export function RealtimeBootstrap({ children }: RealtimeBootstrapProps) {
  const isAuthenticated = useAuthStore((state) => state.session !== null);
  useDuetPresenceSubscription();
  useMessageReceivedSubscription();
  useGroupConversationChangedSubscription();
  useConversationParticipantsAddedSubscription();
  useConversationParticipantsRemovedSubscription();

  useEffect(() => {
    if (!isAuthenticated) return;

    void startRealtimeConnection();
    return stopRealtimeConnection;
  }, [isAuthenticated]);

  return children;
}
