import type { HealthResponse } from "./types";

const fallbackApiBaseUrl = "http://localhost:5000";

export function getApiBaseUrl() {
  return process.env.NEXT_PUBLIC_API_BASE_URL || fallbackApiBaseUrl;
}

export async function fetchApiHealth(signal?: AbortSignal): Promise<HealthResponse> {
  const response = await fetch(`${getApiBaseUrl()}/health`, {
    cache: "no-store",
    signal
  });

  if (!response.ok) {
    throw new Error(`Health check failed with HTTP ${response.status}.`);
  }

  return response.json() as Promise<HealthResponse>;
}
