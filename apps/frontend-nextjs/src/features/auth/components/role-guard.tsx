"use client";

import { useEffect } from "react";
import { usePathname, useRouter } from "next/navigation";
import { useAuth } from "../auth-provider";
import type { RoleName } from "../types";

export function RoleGuard({
  allowedRoles,
  children
}: Readonly<{
  allowedRoles: RoleName[];
  children: React.ReactNode;
}>) {
  const { user, status } = useAuth();
  const router = useRouter();
  const pathname = usePathname();

  useEffect(() => {
    if (status === "anonymous") {
      router.replace(`/login?returnTo=${encodeURIComponent(pathname)}`);
      return;
    }

    if (status === "authenticated" && user && !allowedRoles.includes(user.activeRole)) {
      router.replace("/unauthorized");
    }
  }, [allowedRoles, pathname, router, status, user]);

  if (status === "loading") {
    return <main className="role-page">Loading session...</main>;
  }

  if (!user || !allowedRoles.includes(user.activeRole)) {
    return <main className="role-page">Checking permission...</main>;
  }

  return <>{children}</>;
}
