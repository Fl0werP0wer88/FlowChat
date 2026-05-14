import { useEffect } from "react";
import { useAuthStore } from "../store/authStore";

const refreshLeadTimeMs = 60_000;
const minimumRefreshDelayMs = 1_000;

export function useSessionRefresher() {
  const accessToken = useAuthStore((s) => s.accessToken);
  const expiresAtUtc = useAuthStore((s) => s.expiresAtUtc);
  const refreshToken = useAuthStore((s) => s.refreshToken);
  const refreshTokenExpiresAtUtc = useAuthStore((s) => s.refreshTokenExpiresAtUtc);
  const refreshSession = useAuthStore((s) => s.refreshSession);

  useEffect(() => {
    if (!accessToken) {
      return;
    }

    const expiration = expiresAtUtc ? Date.parse(expiresAtUtc) : null;
    if (expiration === null || Number.isNaN(expiration)) {
      return;
    }

    if (expiration <= Date.now()) {
      void refreshSession();
      return;
    }

    const delay = Math.max(expiration - Date.now() - refreshLeadTimeMs, minimumRefreshDelayMs);
    const id = window.setTimeout(() => void refreshSession(), delay);
    return () => window.clearTimeout(id);
  }, [accessToken, expiresAtUtc, refreshToken, refreshTokenExpiresAtUtc, refreshSession]);
}
