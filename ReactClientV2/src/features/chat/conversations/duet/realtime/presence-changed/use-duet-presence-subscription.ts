import { useQueryClient } from '@tanstack/react-query';
import { useEffect } from 'react';

import {
  subscribeToRealtimeEvent,
  subscribeToRealtimeReconnected,
} from '@/lib/realtime/realtime-client';

import {
  getDuetsWithPresenceQueryOptions,
  type GetDuetsWithPresenceResponse,
} from '../../api/get-duets-with-presence';

import { applyPresenceChanged } from './presence-changed';

export function useDuetPresenceSubscription() {
  const queryClient = useQueryClient();

  useEffect(() => {
    const queryKey = getDuetsWithPresenceQueryOptions().queryKey;
    const handlePresenceChanged = (payload: unknown) => {
      queryClient.setQueryData<GetDuetsWithPresenceResponse>(queryKey, (current) =>
        applyPresenceChanged(current, payload),
      );
    };
    const handleReconnected = () => {
      void queryClient.invalidateQueries({ queryKey });
    };

    const unsubscribePresence = subscribeToRealtimeEvent('PresenceChanged', handlePresenceChanged);
    const unsubscribeReconnected = subscribeToRealtimeReconnected(handleReconnected);

    return () => {
      unsubscribePresence();
      unsubscribeReconnected();
    };
  }, [queryClient]);
}
