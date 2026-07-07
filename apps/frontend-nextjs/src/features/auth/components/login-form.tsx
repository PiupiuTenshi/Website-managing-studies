"use client";

import { FormEvent, useState } from "react";
import { useRouter, useSearchParams } from "next/navigation";
import { useAuth } from "../auth-provider";
import type { RoleName } from "../types";

const roles: RoleName[] = ["Admin", "Manager", "Student", "Parent"];

const roleLanding: Record<RoleName, string> = {
  Admin: "/admin",
  Manager: "/manager",
  Student: "/student",
  Parent: "/parent"
};

export function LoginForm() {
  const router = useRouter();
  const searchParams = useSearchParams();
  const { login } = useAuth();
  const [email, setEmail] = useState("admin@example.test");
  const [password, setPassword] = useState("ChangeMe123!");
  const [role, setRole] = useState<RoleName>("Admin");
  const [error, setError] = useState<string | null>(null);
  const [isSubmitting, setIsSubmitting] = useState(false);

  async function onSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setError(null);
    setIsSubmitting(true);

    try {
      await login({ login: email, password, role });
      const returnTo = searchParams.get("returnTo");
      router.replace(returnTo && returnTo.startsWith("/") ? returnTo : roleLanding[role]);
    } catch (loginError) {
      setError(loginError instanceof Error ? loginError.message : "Login failed.");
    } finally {
      setIsSubmitting(false);
    }
  }

  return (
    <form className="auth-form" onSubmit={onSubmit}>
      <label>
        Email or username
        <input value={email} onChange={(event) => setEmail(event.target.value)} autoComplete="username" />
      </label>

      <label>
        Password
        <input
          value={password}
          onChange={(event) => setPassword(event.target.value)}
          type="password"
          autoComplete="current-password"
        />
      </label>

      <label>
        Role
        <select value={role} onChange={(event) => setRole(event.target.value as RoleName)}>
          {roles.map((item) => (
            <option key={item} value={item}>
              {item}
            </option>
          ))}
        </select>
      </label>

      {error ? <p className="form-error">{error}</p> : null}

      <button className="primary-action" disabled={isSubmitting} type="submit">
        {isSubmitting ? "Signing in..." : "Sign in"}
      </button>
    </form>
  );
}
