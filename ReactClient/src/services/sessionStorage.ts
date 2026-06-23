import type { AuthSession } from "../types/auth";

const accessTokenStorageKey = "flowchat_access_token";
const loginStorageKey = "flowchat_login";
const expirationStorageKey = "flowchat_access_token_expiration";

export interface StoredSession {
  accessToken: string | null;
  login: string | null;
  expiresAtUtc: string | null;
}

const emptySession: StoredSession = {
  accessToken: null,
  login: null,
  expiresAtUtc: null,
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
  };
}

export function storeSession(session: AuthSession): void {
  if (!canUseStorage()) {
    return;
  }

  localStorage.setItem(accessTokenStorageKey, session.accessToken);
  localStorage.setItem(loginStorageKey, session.login);

  if (session.expiresAtUtc) {
    localStorage.setItem(expirationStorageKey, session.expiresAtUtc);
  } else {
    localStorage.removeItem(expirationStorageKey);
  }
}

export function clearStoredSession(): void {
  if (!canUseStorage()) {
    return;
  }

  localStorage.removeItem(accessTokenStorageKey);
  localStorage.removeItem(loginStorageKey);
  localStorage.removeItem(expirationStorageKey);
}
