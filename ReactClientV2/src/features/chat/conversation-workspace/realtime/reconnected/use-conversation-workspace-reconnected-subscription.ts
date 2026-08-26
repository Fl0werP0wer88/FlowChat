import { useEffect, useRef } from 'react';

import { subscribeToRealtimeReconnected } from '@/lib/realtime/realtime-client';

export interface UseConversationWorkspaceReconnectedSubscriptionOptions {
  activeConversationId: string | null;
  onReconnected?: (conversationId: string) => void;
}

export function useConversationWorkspaceReconnectedSubscription(
  options: UseConversationWorkspaceReconnectedSubscriptionOptions,
) {
  const optionsRef = useRef(options);

  useEffect(() => {
    optionsRef.current = options;
  }, [options]);

  useEffect(
    () =>
      subscribeToRealtimeReconnected(() => {
        const { activeConversationId, onReconnected } = optionsRef.current;
        if (activeConversationId) onReconnected?.(activeConversationId);
      }),
    [],
  );
}
