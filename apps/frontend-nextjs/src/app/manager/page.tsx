import { RoleShell } from "@/features/auth/components/role-shell";

export default function ManagerPage() {
  return (
    <RoleShell role="Manager" title="Manager workspace">
      <p>Class, assignment, and grading management start here.</p>
    </RoleShell>
  );
}
