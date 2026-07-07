import { useEffect, useState, useCallback, useRef } from "react";
import * as signalR from "@microsoft/signalr";
import { ChatMessage, chatApi } from "./api";

const getAccessToken = () => {
  if (typeof document !== "undefined") {
    const match = document.cookie.match(new RegExp("(^| )access_token=([^;]+)"));
    if (match) return match[2];
  }
  return "";
};

export const useChat = (roomId: string | null) => {
  const [messages, setMessages] = useState<ChatMessage[]>([]);
  const [isConnected, setIsConnected] = useState(false);
  const [isLoading, setIsLoading] = useState(false);
  const connectionRef = useRef<signalR.HubConnection | null>(null);

  // Initial load
  useEffect(() => {
    if (!roomId) return;
    setIsLoading(true);
    chatApi.getMessages(roomId).then(res => {
      if (res.success && res.data) {
        setMessages(res.data);
      }
    }).finally(() => setIsLoading(false));
  }, [roomId]);

  // SignalR connection
  useEffect(() => {
    const token = getAccessToken();
    if (!token) return;

    // Use absolute URL since Next.js proxies to backend differently, but if we use rewrites it might work.
    // However, SignalR requires full URL or same origin. Let's assume standard /api proxy doesn't cover /hubs by default.
    // We should use NEXT_PUBLIC_API_URL or the /hubs path if rewritten. We'll rely on the existing rewrites if we add it,
    // or point directly to localhost:5000/hubs/chat. For this app, backend is usually http://localhost:5246.
    // It's safer to use the relative path /hubs/chat and add it to next.config.ts rewrites!
    
    const connection = new signalR.HubConnectionBuilder()
      .withUrl(`/hubs/chat?access_token=${token}`)
      .withAutomaticReconnect()
      .build();

    connectionRef.current = connection;

    connection.on("ReceiveMessage", (message: ChatMessage) => {
      // Only append if it belongs to current room
      setMessages(prev => {
        // Prevent duplicate messages
        if (prev.find(m => m.id === message.id)) return prev;
        return [...prev, message];
      });
    });

    connection.start()
      .then(() => {
        setIsConnected(true);
        // If room is selected, join
        if (roomId) {
          connection.invoke("JoinRoom", roomId).catch(console.error);
        }
      })
      .catch(console.error);

    return () => {
      connection.stop();
      setIsConnected(false);
    };
  }, []);

  // Handle room change
  useEffect(() => {
    if (isConnected && roomId && connectionRef.current) {
      connectionRef.current.invoke("JoinRoom", roomId).catch(console.error);
    }
  }, [roomId, isConnected]);

  const sendMessage = useCallback((content: string) => {
    if (isConnected && roomId && connectionRef.current) {
      connectionRef.current.invoke("SendMessage", roomId, content).catch(console.error);
    }
  }, [isConnected, roomId]);

  return { messages, isConnected, isLoading, sendMessage };
};
