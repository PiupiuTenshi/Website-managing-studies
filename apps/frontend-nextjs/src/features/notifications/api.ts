import { getApiBaseUrl } from "@/features/platform-status/api";
import { loadAuthSession } from "@/features/auth/auth-storage";

export interface Notification {
  id: string;
  title: string;
  message: string;
  isRead: boolean;
  createdAt: string;
}

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

  if (response.status === 204) {
    return null;
  }
  return response.json();
}

export async function fetchNotifications(): Promise<Notification[]> {
  return await fetchWithAuth(`${getApiBaseUrl()}/api/notifications`);
}

export async function fetchUnreadCount(): Promise<number> {
  const res = await fetchWithAuth(`${getApiBaseUrl()}/api/notifications/unread-count`);
  return res.count;
}

export async function markAsRead(id: string): Promise<void> {
  await fetchWithAuth(`${getApiBaseUrl()}/api/notifications/${id}/read`, { method: "POST" });
}
