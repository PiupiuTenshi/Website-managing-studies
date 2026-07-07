import { RoleShell } from "@/features/auth/components/role-shell";

export default function StudentPage() {
  return (
    <RoleShell role="Student" title="Student workspace">
      <p>Assigned work, submissions, and feedback start here.</p>
    </RoleShell>
  );
}
