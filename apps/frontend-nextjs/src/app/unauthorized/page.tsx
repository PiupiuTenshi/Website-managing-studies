export default function UnauthorizedPage() {
  return (
    <main className="auth-page">
      <section className="auth-panel">
        <p className="eyebrow">403</p>
        <h1>Access denied</h1>
        <p className="summary">Your current role cannot open this workspace.</p>
      </section>
    </main>
  );
}
