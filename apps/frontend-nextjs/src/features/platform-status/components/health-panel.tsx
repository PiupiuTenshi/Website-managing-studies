"use client";

import { useEffect, useState } from "react";
import { fetchApiHealth, getApiBaseUrl } from "../api";
import type { HealthResponse } from "../types";

type HealthState =
  | { status: "idle"; data: null; error: null }
  | { status: "ok"; data: HealthResponse; error: null }
  | { status: "error"; data: null; error: string };

export function HealthPanel() {
  const [state, setState] = useState<HealthState>({
    status: "idle",
    data: null,
    error: null
  });

  async function loadHealth(signal?: AbortSignal) {
    setState({ status: "idle", data: null, error: null });

    try {
      const data = await fetchApiHealth(signal);
      setState({ status: "ok", data, error: null });
    } catch (error) {
      if (error instanceof DOMException && error.name === "AbortError") {
        return;
      }

      setState({
        status: "error",
        data: null,
        error: error instanceof Error ? error.message : "Health check failed."
      });
    }
  }

  useEffect(() => {
    const controller = new AbortController();
    void loadHealth(controller.signal);

    return () => controller.abort();
  }, []);

  const badgeClassName = `badge ${state.status}`;

  return (
    <aside className="health-panel" aria-label="API health status">
      <div className="panel-title">
        <h2>API health</h2>
        <span className={badgeClassName}>{state.status}</span>
      </div>

      <dl className="health-details">
        <div className="health-row">
          <dt>API base URL</dt>
          <dd>{getApiBaseUrl()}</dd>
        </div>
        <div className="health-row">
          <dt>Service</dt>
          <dd>{state.data?.service ?? "-"}</dd>
        </div>
        <div className="health-row">
          <dt>Environment</dt>
          <dd>{state.data?.environment ?? "-"}</dd>
        </div>
        <div className="health-row">
          <dt>Database</dt>
          <dd>{state.data ? (state.data.databaseConfigured ? "configured" : "not configured") : "-"}</dd>
        </div>
      </dl>

      <div className="health-actions">
        <button className="icon-button" type="button" onClick={() => void loadHealth()} title="Refresh API health">
          R
        </button>
      </div>

      {state.error ? <p className="health-note">{state.error}</p> : null}
    </aside>
  );
}
