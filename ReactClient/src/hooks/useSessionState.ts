import { useMemo, useState } from "react";
import { clearStoredSession, loadStoredSession, storeSession, type StoredSession } from "../services/sessionStorage";
import type { AuthSession } from "../types/auth";

const emptySession: StoredSession = {
  accessToken: null,
  login: null,
  expiresAtUtc: null
};

export function useSessionState() {
  const initialSession = useMemo(() => loadStoredSession(), []);
  const [session, setSession] = useState<StoredSession>(initialSession);

  const signIn = (nextSession: AuthSession) => {
    storeSession(nextSession);
    setSession({
      accessToken: nextSession.accessToken,
      login: nextSession.login,
      expiresAtUtc: nextSession.expiresAtUtc
    });
  };

  const signOut = () => {
    clearStoredSession();
    setSession(emptySession);
  };

  return {
    session,
    signIn,
    signOut,
    isAuthenticated: Boolean(session.accessToken)
  };
}
