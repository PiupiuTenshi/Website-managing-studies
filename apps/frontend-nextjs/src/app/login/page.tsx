import { Suspense } from "react";
import { LoginForm } from "@/features/auth/components/login-form";

export default function LoginPage() {
  return (
    <main className="auth-page">
      <section className="auth-panel">
        <p className="eyebrow">Phase 1</p>
        <h1>Sign in</h1>
        <p className="summary">Use a role account to test JWT, refresh session, and route guards.</p>
        <Suspense fallback={<p className="summary">Loading login form...</p>}>
          <LoginForm />
        </Suspense>
      </section>
    </main>
  );
}
