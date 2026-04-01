import { useEffect, useEffectEvent, useMemo, useRef, useState } from "react";
import { refreshUserSession } from "../features/auth/api";
import { clearStoredSession, loadStoredSession, type StoredSession, storeSession } from "../services/sessionStorage";
import type { AuthSession } from "../types/auth";

const refreshLeadTimeMs = 60_000;
const minimumRefreshDelayMs = 1_000;

const emptySession: StoredSession = {
  accessToken: null,
  login: null,
  expiresAtUtc: null,
  refreshToken: null,
  refreshTokenExpiresAtUtc: null,
};

function mapAuthSessionToStoredSession(session: AuthSession): StoredSession {
  return {
    accessToken: session.accessToken,
    login: session.login,
    expiresAtUtc: session.expiresAtUtc,
    refreshToken: session.refreshToken,
    refreshTokenExpiresAtUtc: session.refreshTokenExpiresAtUtc,
  };
}

function parseUtcDate(value: string | null): number | null {
  if (!value) {
    return null;
  }

  const parsedValue = Date.parse(value);
  return Number.isNaN(parsedValue) ? null : parsedValue;
}

function canRefreshSession(
  session: StoredSession,
): session is StoredSession & {
  accessToken: string;
  login: string;
  refreshToken: string;
} {
  return Boolean(session.accessToken && session.login && session.refreshToken);
}

export function useSessionState() {
  const initialSession = useMemo(() => loadStoredSession(), []);
  const [session, setSession] = useState<StoredSession>(initialSession);
  const sessionRef = useRef(initialSession);
  const refreshPromiseRef = useRef<Promise<AuthSession | null> | null>(null);

  useEffect(() => {
    sessionRef.current = session;
  }, [session]);

  const replaceSession = useEffectEvent((nextSession: AuthSession) => {
    const storedSession = mapAuthSessionToStoredSession(nextSession);
    sessionRef.current = storedSession;
    storeSession(nextSession);
    setSession(storedSession);
  });

  const clearSession = useEffectEvent(() => {
    sessionRef.current = emptySession;
    clearStoredSession();
    setSession(emptySession);
  });

  const refreshSession = useEffectEvent(async (): Promise<AuthSession | null> => {
    if (refreshPromiseRef.current) {
      return refreshPromiseRef.current;
    }

    const currentSession = sessionRef.current;
    if (!canRefreshSession(currentSession)) {
      clearSession();
      return null;
    }

    const refreshTokenExpiration = parseUtcDate(currentSession.refreshTokenExpiresAtUtc);
    if (refreshTokenExpiration !== null && refreshTokenExpiration <= Date.now()) {
      clearSession();
      return null;
    }

    const currentAccessToken = currentSession.accessToken;
    const currentRefreshToken = currentSession.refreshToken;

    const refreshPromise = refreshUserSession({
      accessToken: currentAccessToken,
      refreshToken: currentRefreshToken,
      login: currentSession.login,
    })
      .then((nextSession) => {
        const latestSession = sessionRef.current;
        if (
          latestSession.accessToken !== currentAccessToken
          || latestSession.refreshToken !== currentRefreshToken
        ) {
          return null;
        }

        replaceSession(nextSession);
        return nextSession;
      })
      .catch(() => {
        const latestSession = sessionRef.current;
        if (
          latestSession.accessToken === currentAccessToken
          && latestSession.refreshToken === currentRefreshToken
        ) {
          clearSession();
        }

        return null;
      })
      .finally(() => {
        if (refreshPromiseRef.current === refreshPromise) {
          refreshPromiseRef.current = null;
        }
      });

    refreshPromiseRef.current = refreshPromise;
    return refreshPromise;
  });

  const signIn = (nextSession: AuthSession) => {
    replaceSession(nextSession);
  };

  const signOut = () => {
    clearSession();
  };

  useEffect(() => {
    if (!session.accessToken) {
      return;
    }

    const accessTokenExpiration = parseUtcDate(session.expiresAtUtc);
    if (accessTokenExpiration === null) {
      return;
    }

    if (accessTokenExpiration <= Date.now()) {
      void refreshSession();
      return;
    }

    const refreshDelay = Math.max(accessTokenExpiration - Date.now() - refreshLeadTimeMs, minimumRefreshDelayMs);
    const timeoutId = window.setTimeout(() => {
      void refreshSession();
    }, refreshDelay);

    return () => {
      window.clearTimeout(timeoutId);
    };
  }, [session.accessToken, session.expiresAtUtc, session.refreshToken, session.refreshTokenExpiresAtUtc]);

  return {
    session,
    signIn,
    signOut,
    isAuthenticated: Boolean(session.accessToken),
  };
}
