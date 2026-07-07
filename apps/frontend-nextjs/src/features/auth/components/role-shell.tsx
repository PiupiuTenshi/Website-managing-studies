"use client";

import { useAuth } from "../auth-provider";
import type { RoleName } from "../types";
import { RoleGuard } from "./role-guard";
import { NotificationBell } from "@/features/notifications/components/NotificationBell";

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
          <div className="flex items-center gap-4">
            <a href="/chat" className="text-blue-600 hover:underline font-medium flex items-center gap-1">
              💬 Chat
            </a>
            <NotificationBell />
            <button className="secondary-action" type="button" onClick={() => void logout()}>
              Sign out
            </button>
          </div>
        </header>
        <section className="role-content">{children}</section>
      </main>
    </RoleGuard>
  );
}
