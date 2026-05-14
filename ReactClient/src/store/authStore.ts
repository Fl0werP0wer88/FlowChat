import { create } from "zustand";
import { refreshUserSession } from "../features/auth/api";
import {
  clearStoredSession,
  loadStoredSession,
  type StoredSession,
  storeSession,
} from "../services/sessionStorage";
import type { AuthSession } from "../types/auth";

// Deduplication outside the store — not reactive state, just a mutex for the async refresh call
let refreshPromise: Promise<AuthSession | null> | null = null;

const emptySession: StoredSession = {
  accessToken: null,
  login: null,
  expiresAtUtc: null,
  refreshToken: null,
  refreshTokenExpiresAtUtc: null,
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
      refreshToken: session.refreshToken,
      refreshTokenExpiresAtUtc: session.refreshTokenExpiresAtUtc,
      isAuthenticated: true,
    });
  },

  signOut: () => {
    clearStoredSession();
    set({ ...emptySession, isAuthenticated: false });
  },

  refreshSession: async (): Promise<AuthSession | null> => {
    if (refreshPromise) {
      return refreshPromise;
    }

    const { accessToken, refreshToken, login, refreshTokenExpiresAtUtc } = get();

    if (!accessToken || !refreshToken || !login) {
      get().signOut();
      return null;
    }

    if (refreshTokenExpiresAtUtc) {
      const expiration = Date.parse(refreshTokenExpiresAtUtc);
      if (!Number.isNaN(expiration) && expiration <= Date.now()) {
        get().signOut();
        return null;
      }
    }

    const capturedAccessToken = accessToken;
    const capturedRefreshToken = refreshToken;

    refreshPromise = refreshUserSession({ accessToken, refreshToken, login })
      .then((nextSession) => {
        const current = get();
        if (
          current.accessToken !== capturedAccessToken
          || current.refreshToken !== capturedRefreshToken
        ) {
          return null;
        }

        get().signIn(nextSession);
        return nextSession;
      })
      .catch(() => {
        const current = get();
        if (
          current.accessToken === capturedAccessToken
          && current.refreshToken === capturedRefreshToken
        ) {
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
