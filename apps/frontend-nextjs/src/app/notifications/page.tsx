"use client";

import { useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import { fetchNotifications, markAsRead, type Notification } from "@/features/notifications/api";
import { RoleShell } from "@/features/auth/components/role-shell";
import { useAuth } from "@/features/auth/auth-provider";

export default function NotificationsPage() {
  const { user } = useAuth();
  const [notifications, setNotifications] = useState<Notification[]>([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    if (user) {
      fetchNotifications()
        .then(data => setNotifications(data || []))
        .catch(err => console.error(err))
        .finally(() => setLoading(false));
    }
  }, [user]);

  const handleMarkAsRead = async (id: string) => {
    try {
      await markAsRead(id);
      setNotifications(prev => prev.map(n => n.id === id ? { ...n, isRead: true } : n));
    } catch (err) {
      console.error(err);
    }
  };

  if (!user || user.roles.length === 0) return null;

  return (
    <RoleShell role={user.roles[0]} title="Thông báo">
      <div className="p-8 max-w-4xl mx-auto">
        <h1 className="text-2xl font-bold text-gray-900 mb-6">Tất cả thông báo</h1>

        {loading ? (
          <p>Đang tải...</p>
        ) : notifications.length === 0 ? (
          <p className="text-gray-500">Bạn không có thông báo nào.</p>
        ) : (
          <div className="space-y-4">
            {notifications.map(n => (
              <div 
                key={n.id} 
                className={`p-4 rounded-lg border ${n.isRead ? 'bg-white border-gray-200' : 'bg-blue-50 border-blue-200'} shadow-sm flex items-start justify-between`}
              >
                <div>
                  <h3 className={`font-semibold ${n.isRead ? 'text-gray-700' : 'text-blue-900'}`}>{n.title}</h3>
                  <div className="text-gray-600 mt-1 text-sm prose" dangerouslySetInnerHTML={{ __html: n.message }}></div>
                  <p className="text-xs text-gray-400 mt-2">{new Date(n.createdAt).toLocaleString()}</p>
                </div>
                {!n.isRead && (
                  <button 
                    onClick={() => handleMarkAsRead(n.id)}
                    className="text-xs text-blue-600 hover:text-blue-800 font-medium px-2 py-1 bg-white rounded border border-blue-200"
                  >
                    Đánh dấu đã đọc
                  </button>
                )}
              </div>
            ))}
          </div>
        )}
      </div>
    </RoleShell>
  );
}
