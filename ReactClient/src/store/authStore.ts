import { create } from "zustand";
import { queryClient } from "../api/queryClient";
import { logoutUser, refreshUserSession } from "../features/auth/api";
import { clearStoredSession, loadStoredSession, type StoredSession, storeSession } from "../services/sessionStorage";
import type { AuthSession } from "../types/auth";

// Deduplication outside the store — not reactive state, just a mutex for the async refresh call
let refreshPromise: Promise<AuthSession | null> | null = null;

const emptySession: StoredSession = {
  accessToken: null,
  login: null,
  expiresAtUtc: null,
};

interface AuthStore extends StoredSession {
  isAuthenticated: boolean;
  signIn: (session: AuthSession) => void;
  signOut: () => void;
  refreshSession: () => Promise<AuthSession | null>;
}

export const useAuthStore = create<AuthStore>((set, get) => ({
  ...loadStoredSession(),
  isAuthenticated: Boolean(loadStoredSession().accessToken),

  signIn: (session: AuthSession) => {
    storeSession(session);
    set({
      accessToken: session.accessToken,
      login: session.login,
      expiresAtUtc: session.expiresAtUtc,
      isAuthenticated: true,
    });
  },

  signOut: () => {
    clearStoredSession();
    set({ ...emptySession, isAuthenticated: false });
    queryClient.clear();
    // Best-effort — clears the HttpOnly refresh token cookie on the server
    logoutUser().catch(() => undefined);
  },

  refreshSession: async (): Promise<AuthSession | null> => {
    if (refreshPromise) {
      return refreshPromise;
    }

    const { accessToken, login } = get();

    if (!accessToken || !login) {
      get().signOut();
      return null;
    }

    const capturedAccessToken = accessToken;

    refreshPromise = refreshUserSession(login)
      .then((nextSession) => {
        const current = get();
        if (current.accessToken !== capturedAccessToken) {
          return null;
        }

        get().signIn(nextSession);
        return nextSession;
      })
      .catch(() => {
        const current = get();
        if (current.accessToken === capturedAccessToken) {
          get().signOut();
        }

        return null;
      })
      .finally(() => {
        refreshPromise = null;
      });

    return refreshPromise;
  },
}));
