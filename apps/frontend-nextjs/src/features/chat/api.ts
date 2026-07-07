import { getApiBaseUrl } from "@/features/platform-status/api";
import { loadAuthSession } from "@/features/auth/auth-storage";

export interface ChatRoom {
  id: string;
  name: string;
  type: string;
  referenceId?: string | null;
}

export interface ChatMessage {
  id: string;
  chatRoomId: string;
  senderId: string;
  senderName: string;
  content: string;
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
    throw new Error(`Request failed with HTTP ${response.status}.`);
  }
  
  return response.json();
}

export const chatApi = {
  getRooms: async () => {
    try {
      const data = await fetchWithAuth(`${getApiBaseUrl()}/api/chat/rooms`);
      return { success: true, data: data as ChatRoom[] };
    } catch (err: any) {
      return { success: false, error: err.message };
    }
  },
  
  getMessages: async (roomId: string) => {
    try {
      const data = await fetchWithAuth(`${getApiBaseUrl()}/api/chat/rooms/${roomId}/messages`);
      return { success: true, data: data as ChatMessage[] };
    } catch (err: any) {
      return { success: false, error: err.message };
    }
  }
};
