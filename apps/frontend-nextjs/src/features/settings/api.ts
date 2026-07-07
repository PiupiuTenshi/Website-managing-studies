import { getApiBaseUrl } from "@/features/platform-status/api";
import { loadAuthSession } from "@/features/auth/auth-storage";
import type { ApiResponse } from "@/features/auth/types";

async function fetchWithAuth(url: string, options: RequestInit = {}) {
  const session = loadAuthSession();
  const token = session?.accessToken;
  if (!token) {
    throw new Error("No auth token available.");
  }

  const headers = new Headers(options.headers);
  headers.set("Authorization", `Bearer ${token}`);
  if (!headers.has("Content-Type") && options.method !== "GET" && options.method !== "DELETE") {
    headers.set("Content-Type", "application/json");
  }

  const response = await fetch(url, { ...options, headers });
  
  if (!response.ok) {
    let errStr = response.statusText;
    try {
      const errJson = await response.json();
      errStr = errJson?.error?.message || errStr;
    } catch { }
    throw new Error(`API Error ${response.status}: ${errStr}`);
  }

  return response.json();
}

export interface TestEmailRequest {
  toEmail: string;
  toName: string;
  subject: string;
  bodyHtml: string;
}

export async function testSmtp(request: TestEmailRequest): Promise<void> {
  await fetchWithAuth(`${getApiBaseUrl()}/api/admin/smtp/test`, {
    method: "POST",
    body: JSON.stringify(request)
  });
}
