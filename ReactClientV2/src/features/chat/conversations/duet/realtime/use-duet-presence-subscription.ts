import { useQueryClient } from '@tanstack/react-query';
import { useEffect } from 'react';

import {
  subscribeToRealtimeEvent,
  subscribeToRealtimeReconnected,
} from '@/lib/realtime/realtime-client';

import {
  getDuetsWithPresenceQueryOptions,
  type GetDuetsWithPresenceResponse,
} from '../api/get-duets-with-presence';

import { applyPresenceChanged } from './presence-changed';

export function useDuetPresenceSubscription() {
  const queryClient = useQueryClient();

  useEffect(() => {
    const queryKey = getDuetsWithPresenceQueryOptions().queryKey;
    const unsubscribePresence = subscribeToRealtimeEvent('PresenceChanged', (payload) => {
      queryClient.setQueryData<GetDuetsWithPresenceResponse>(queryKey, (current) =>
        applyPresenceChanged(current, payload),
      );
    });
    const unsubscribeReconnected = subscribeToRealtimeReconnected(() => {
      void queryClient.invalidateQueries({ queryKey });
    });

    return () => {
      unsubscribePresence();
      unsubscribeReconnected();
    };
  }, [queryClient]);
}
