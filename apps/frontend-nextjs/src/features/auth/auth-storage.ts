import type { AuthTokenResponse } from "./types";

const storageKey = "remote-assignment-auth";

export function saveAuthSession(session: AuthTokenResponse) {
  window.sessionStorage.setItem(storageKey, JSON.stringify(session));
}

export function loadAuthSession(): AuthTokenResponse | null {
  const value = window.sessionStorage.getItem(storageKey);
  if (!value) {
    return null;
  }

  try {
    return JSON.parse(value) as AuthTokenResponse;
  } catch {
    window.sessionStorage.removeItem(storageKey);
    return null;
  }
}

export function clearAuthSession() {
  window.sessionStorage.removeItem(storageKey);
}
