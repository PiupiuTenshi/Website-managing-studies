import { RoleShell } from "@/features/auth/components/role-shell";

export default function ParentPage() {
  return (
    <RoleShell role="Parent" title="Parent workspace">
      <p>Child progress, grades, and alerts start here.</p>
    </RoleShell>
  );
}
