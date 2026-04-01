import type { AuthSession } from "../types/auth";

const accessTokenStorageKey = "flowchat_access_token";
const loginStorageKey = "flowchat_login";
const expirationStorageKey = "flowchat_access_token_expiration";
const refreshTokenStorageKey = "flowchat_refresh_token";
const refreshTokenExpirationStorageKey = "flowchat_refresh_token_expiration";

export interface StoredSession {
  accessToken: string | null;
  login: string | null;
  expiresAtUtc: string | null;
  refreshToken: string | null;
  refreshTokenExpiresAtUtc: string | null;
}

const emptySession: StoredSession = {
  accessToken: null,
  login: null,
  expiresAtUtc: null,
  refreshToken: null,
  refreshTokenExpiresAtUtc: null,
};

function canUseStorage(): boolean {
  return typeof window !== "undefined" && typeof localStorage !== "undefined";
}

export function loadStoredSession(): StoredSession {
  if (!canUseStorage()) {
    return emptySession;
  }

  return {
    accessToken: localStorage.getItem(accessTokenStorageKey),
    login: localStorage.getItem(loginStorageKey),
    expiresAtUtc: localStorage.getItem(expirationStorageKey),
    refreshToken: localStorage.getItem(refreshTokenStorageKey),
    refreshTokenExpiresAtUtc: localStorage.getItem(refreshTokenExpirationStorageKey),
  };
}

export function storeSession(session: AuthSession): void {
  if (!canUseStorage()) {
    return;
  }

  localStorage.setItem(accessTokenStorageKey, session.accessToken);
  localStorage.setItem(loginStorageKey, session.login);
  localStorage.setItem(refreshTokenStorageKey, session.refreshToken);

  if (session.expiresAtUtc) {
    localStorage.setItem(expirationStorageKey, session.expiresAtUtc);
  } else {
    localStorage.removeItem(expirationStorageKey);
  }

  if (session.refreshTokenExpiresAtUtc) {
    localStorage.setItem(refreshTokenExpirationStorageKey, session.refreshTokenExpiresAtUtc);
  } else {
    localStorage.removeItem(refreshTokenExpirationStorageKey);
  }
}

export function clearStoredSession(): void {
  if (!canUseStorage()) {
    return;
  }

  localStorage.removeItem(accessTokenStorageKey);
  localStorage.removeItem(loginStorageKey);
  localStorage.removeItem(expirationStorageKey);
  localStorage.removeItem(refreshTokenStorageKey);
  localStorage.removeItem(refreshTokenExpirationStorageKey);
}
