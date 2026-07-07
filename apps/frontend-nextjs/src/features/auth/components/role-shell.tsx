"use client";

import { useAuth } from "../auth-provider";
import type { RoleName } from "../types";
import { RoleGuard } from "./role-guard";

export function RoleShell({
  role,
  title,
  children
}: Readonly<{
  role: RoleName;
  title: string;
  children: React.ReactNode;
}>) {
  const { user, logout } = useAuth();

  return (
    <RoleGuard allowedRoles={[role]}>
      <main className="role-page">
        <header className="role-header">
          <div>
            <p className="eyebrow">{role}</p>
            <h1>{title}</h1>
            <p className="summary">
              Signed in as {user?.fullName} ({user?.email})
            </p>
          </div>
          <button className="secondary-action" type="button" onClick={() => void logout()}>
            Sign out
          </button>
        </header>
        <section className="role-content">{children}</section>
      </main>
    </RoleGuard>
  );
}
