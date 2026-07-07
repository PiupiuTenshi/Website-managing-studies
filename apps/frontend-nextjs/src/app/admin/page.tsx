import { RoleShell } from "@/features/auth/components/role-shell";

export default function AdminPage() {
  return (
    <RoleShell role="Admin" title="Admin workspace">
      <p>Account lock/unlock and role administration start here.</p>
    </RoleShell>
  );
}
