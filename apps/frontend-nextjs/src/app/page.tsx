import { HealthPanel } from "@/features/platform-status/components/health-panel";
import Link from "next/link";

const setupItems = [
  "ASP.NET Core Web API health endpoint",
  "Next.js App Router frontend",
  "Supabase PostgreSQL schema baseline",
  "AI-agent rules and implementation handbook"
];

export default function HomePage() {
  return (
    <main className="page-shell">
      <section className="workspace-header">
        <div>
          <p className="eyebrow">Phase 0</p>
          <h1>Remote Assignment Platform</h1>
          <p className="summary">
            Nen tang ban dau cho he thong giao bai, nop bai va cham diem tu xa.
          </p>
          <div className="home-actions">
            <Link className="primary-link" href="/login">
              Open login
            </Link>
          </div>
        </div>
        <HealthPanel />
      </section>

      <section className="setup-grid" aria-label="Phase 0 setup status">
        {setupItems.map((item) => (
          <article className="setup-item" key={item}>
            <span aria-hidden="true" className="status-dot" />
            <p>{item}</p>
          </article>
        ))}
      </section>
    </main>
  );
}
