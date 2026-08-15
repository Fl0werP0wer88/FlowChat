import { useEffect } from 'react';

import { refreshSession } from '@/lib/auth-session';
import { useAuthStore } from '@/stores/auth-store';

const refreshLeadTimeMs = 60_000;
const minimumRefreshDelayMs = 1_000;

export function useSessionRefresher() {
  const expiresAtUtc = useAuthStore((state) => state.session?.expiresAtUtc ?? null);

  useEffect(() => {
    if (!expiresAtUtc) return;

    const expiration = Date.parse(expiresAtUtc);
    if (Number.isNaN(expiration)) return;

    const delay = Math.max(expiration - Date.now() - refreshLeadTimeMs, minimumRefreshDelayMs);
    const timeoutId = window.setTimeout(() => void refreshSession(), delay);

    return () => window.clearTimeout(timeoutId);
  }, [expiresAtUtc]);
}
