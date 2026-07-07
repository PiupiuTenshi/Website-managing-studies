"use client";

import { useEffect, useState, useRef } from "react";
import { ChatRoom, chatApi } from "@/features/chat/api";
import { useChat } from "@/features/chat/use-chat";
import { format } from "date-fns";

export default function ChatPage() {
  const [rooms, setRooms] = useState<ChatRoom[]>([]);
  const [activeRoomId, setActiveRoomId] = useState<string | null>(null);
  const [inputMessage, setInputMessage] = useState("");
  
  const { messages, isConnected, isLoading, sendMessage } = useChat(activeRoomId);
  const messagesEndRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    chatApi.getRooms().then(res => {
      if (res.success && res.data) {
        setRooms(res.data);
        if (res.data.length > 0) {
          setActiveRoomId(res.data[0].id);
        }
      }
    });
  }, []);

  useEffect(() => {
    // Scroll to bottom when messages change
    messagesEndRef.current?.scrollIntoView({ behavior: "smooth" });
  }, [messages]);

  const handleSend = (e: React.FormEvent) => {
    e.preventDefault();
    if (!inputMessage.trim()) return;
    sendMessage(inputMessage.trim());
    setInputMessage("");
  };

  return (
    <div className="flex h-[calc(100vh-4rem)] max-w-7xl mx-auto rounded-lg overflow-hidden border shadow-sm mt-4">
      {/* Sidebar: Room List */}
      <div className="w-1/3 bg-gray-50 border-r flex flex-col">
        <div className="p-4 border-b bg-white">
          <h2 className="font-semibold text-lg">Phòng Chat</h2>
        </div>
        <div className="overflow-y-auto flex-1">
          {rooms.map(room => (
            <div
              key={room.id}
              onClick={() => setActiveRoomId(room.id)}
              className={`p-4 border-b cursor-pointer hover:bg-gray-100 transition-colors ${activeRoomId === room.id ? 'bg-blue-50 border-l-4 border-l-blue-500' : ''}`}
            >
              <h3 className="font-medium text-gray-900">{room.name}</h3>
              <p className="text-sm text-gray-500">{room.type}</p>
            </div>
          ))}
          {rooms.length === 0 && (
            <div className="p-4 text-center text-gray-500 text-sm">
              Bạn chưa tham gia phòng chat nào.
            </div>
          )}
        </div>
      </div>

      {/* Main Chat Area */}
      <div className="flex-1 flex flex-col bg-white">
        {activeRoomId ? (
          <>
            <div className="p-4 border-b flex justify-between items-center shadow-sm">
              <h2 className="font-semibold">{rooms.find(r => r.id === activeRoomId)?.name || 'Đang tải...'}</h2>
              <div className="flex items-center gap-2">
                <span className={`w-2 h-2 rounded-full ${isConnected ? 'bg-green-500' : 'bg-red-500'}`}></span>
                <span className="text-xs text-gray-500">{isConnected ? 'Đã kết nối' : 'Mất kết nối'}</span>
              </div>
            </div>

            <div className="flex-1 overflow-y-auto p-4 space-y-4">
              {isLoading ? (
                <div className="flex justify-center p-4">
                  <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-blue-500"></div>
                </div>
              ) : messages.length === 0 ? (
                <div className="h-full flex items-center justify-center text-gray-400">
                  Chưa có tin nhắn nào. Hãy là người đầu tiên!
                </div>
              ) : (
                messages.map(msg => {
                  // In a real app we'd compare senderId with currentUser ID to align left/right
                  // For now we'll just show them simply
                  return (
                    <div key={msg.id} className="flex flex-col mb-4">
                      <div className="flex items-baseline gap-2 mb-1">
                        <span className="font-semibold text-sm">{msg.senderName}</span>
                        <span className="text-xs text-gray-500">{format(new Date(msg.createdAt), 'HH:mm')}</span>
                      </div>
                      <div className="bg-gray-100 p-3 rounded-lg rounded-tl-none inline-block max-w-[80%] break-words">
                        {msg.content}
                      </div>
                    </div>
                  );
                })
              )}
              <div ref={messagesEndRef} />
            </div>

            <form onSubmit={handleSend} className="p-4 border-t bg-gray-50 flex gap-2">
              <input
                type="text"
                value={inputMessage}
                onChange={e => setInputMessage(e.target.value)}
                placeholder="Nhập tin nhắn..."
                className="flex-1 px-4 py-2 border rounded-full focus:outline-none focus:ring-2 focus:ring-blue-500"
                disabled={!isConnected}
              />
              <button
                type="submit"
                disabled={!isConnected || !inputMessage.trim()}
                className="px-6 py-2 bg-blue-600 text-white rounded-full font-medium hover:bg-blue-700 disabled:opacity-50 disabled:cursor-not-allowed transition-colors"
              >
                Gửi
              </button>
            </form>
          </>
        ) : (
          <div className="h-full flex items-center justify-center text-gray-400">
            Chọn một phòng chat để bắt đầu
          </div>
        )}
      </div>
    </div>
  );
}
