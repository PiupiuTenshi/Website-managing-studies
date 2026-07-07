"use client";

import { useEffect, useState } from "react";
import Link from "next/link";
import { fetchUnreadCount } from "../api";
import { loadAuthSession } from "@/features/auth/auth-storage";

export function NotificationBell() {
  const [count, setCount] = useState(0);

  useEffect(() => {
    const session = loadAuthSession();
    if (!session) return;

    fetchUnreadCount().then(c => setCount(c)).catch(err => console.error("Failed to fetch unread count", err));

    const interval = setInterval(() => {
      fetchUnreadCount().then(c => setCount(c)).catch(err => console.error(err));
    }, 60000); // Check every minute

    return () => clearInterval(interval);
  }, []);

  return (
    <Link href="/notifications" className="relative p-2 text-gray-500 hover:text-indigo-600 transition-colors" title="Thông báo">
      <svg className="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
        <path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" d="M15 17h5l-1.405-1.405A2.032 2.032 0 0118 14.158V11a6.002 6.002 0 00-4-5.659V5a2 2 0 10-4 0v.341C7.67 6.165 6 8.388 6 11v3.159c0 .538-.214 1.055-.595 1.436L4 17h5m6 0v1a3 3 0 11-6 0v-1m6 0H9"></path>
      </svg>
      {count > 0 && (
        <span className="absolute top-1 right-1 inline-flex items-center justify-center px-2 py-1 text-xs font-bold leading-none text-red-100 transform translate-x-1/4 -translate-y-1/4 bg-red-600 rounded-full">
          {count > 99 ? '99+' : count}
        </span>
      )}
    </Link>
  );
}
