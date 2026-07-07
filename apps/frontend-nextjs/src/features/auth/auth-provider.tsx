"use client";

import { createContext, useContext, useEffect, useMemo, useState } from "react";
import { fetchCurrentUser, login as loginRequest, logout as logoutRequest } from "./api";
import { clearAuthSession, loadAuthSession, saveAuthSession } from "./auth-storage";
import type { AuthTokenResponse, AuthUser, LoginInput } from "./types";

type AuthContextValue = {
  user: AuthUser | null;
  accessToken: string | null;
  refreshToken: string | null;
  status: "loading" | "authenticated" | "anonymous";
  login: (input: LoginInput) => Promise<void>;
  logout: () => Promise<void>;
};

const AuthContext = createContext<AuthContextValue | null>(null);

export function AuthProvider({ children }: Readonly<{ children: React.ReactNode }>) {
  const [session, setSession] = useState<AuthTokenResponse | null>(null);
  const [status, setStatus] = useState<AuthContextValue["status"]>("loading");

  useEffect(() => {
    const stored = loadAuthSession();
    if (!stored) {
      setStatus("anonymous");
      return;
    }

    setSession(stored);
    setStatus("authenticated");

    void fetchCurrentUser(stored.accessToken).catch(() => {
      clearAuthSession();
      setSession(null);
      setStatus("anonymous");
    });
  }, []);

  async function handleLogin(input: LoginInput) {
    const nextSession = await loginRequest(input);
    saveAuthSession(nextSession);
    setSession(nextSession);
    setStatus("authenticated");
  }

  async function handleLogout() {
    const current = session;
    clearAuthSession();
    setSession(null);
    setStatus("anonymous");

    if (current) {
      await logoutRequest(current.accessToken, current.refreshToken).catch(() => undefined);
    }
  }

  const value = useMemo<AuthContextValue>(
    () => ({
      user: session?.user ?? null,
      accessToken: session?.accessToken ?? null,
      refreshToken: session?.refreshToken ?? null,
      status,
      login: handleLogin,
      logout: handleLogout
    }),
    [session, status]
  );

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth() {
  const context = useContext(AuthContext);
  if (!context) {
    throw new Error("useAuth must be used inside AuthProvider.");
  }

  return context;
}
