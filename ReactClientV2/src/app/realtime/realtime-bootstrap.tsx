import { useEffect, type ReactNode } from 'react';

import { useDuetPresenceSubscription } from '@/features/chat/conversations/duet/realtime/presence-changed/use-duet-presence-subscription';
import { startRealtimeConnection, stopRealtimeConnection } from '@/lib/realtime/realtime-client';
import { useAuthStore } from '@/stores/auth-store';

interface RealtimeBootstrapProps {
  children: ReactNode;
}

export function RealtimeBootstrap({ children }: RealtimeBootstrapProps) {
  const isAuthenticated = useAuthStore((state) => state.session !== null);
  useDuetPresenceSubscription();

  useEffect(() => {
    if (!isAuthenticated) return;

    void startRealtimeConnection();
    return stopRealtimeConnection;
  }, [isAuthenticated]);

  return children;
}
