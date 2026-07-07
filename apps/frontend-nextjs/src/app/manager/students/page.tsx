"use client";

import { RoleShell } from "@/features/auth/components/role-shell";
import { useState } from "react";

export default function ManagerStudentsPage() {
  const [activeTab, setActiveTab] = useState<"students" | "parents">("students");

  return (
    <RoleShell role="Manager" title="Quản lý Học sinh & Phụ huynh">
      <div className="p-8">
        <div className="flex justify-between items-center mb-8">
          <div className="flex gap-4">
            <button className="px-4 py-2 bg-white border border-gray-300 rounded shadow-sm hover:bg-gray-50">
              Import danh sách (CSV)
            </button>
            <button className="px-4 py-2 bg-indigo-600 text-white rounded shadow-sm hover:bg-indigo-700">
              + Thêm học sinh
            </button>
          </div>
        </div>

        {/* Tabs */}
        <div className="border-b border-gray-200 mb-6">
          <nav className="-mb-px flex space-x-8">
            <button
              onClick={() => setActiveTab("students")}
              className={`pb-4 px-1 border-b-2 font-medium text-sm ${
                activeTab === "students"
                  ? "border-indigo-500 text-indigo-600"
                  : "border-transparent text-gray-500 hover:text-gray-700 hover:border-gray-300"
              }`}
            >
              Danh sách Học sinh
            </button>
            <button
              onClick={() => setActiveTab("parents")}
              className={`pb-4 px-1 border-b-2 font-medium text-sm ${
                activeTab === "parents"
                  ? "border-indigo-500 text-indigo-600"
                  : "border-transparent text-gray-500 hover:text-gray-700 hover:border-gray-300"
              }`}
            >
              Liên kết Phụ huynh
            </button>
          </nav>
        </div>

        {/* Content Placeholder */}
        <div className="bg-white p-12 text-center rounded-lg border border-gray-200 border-dashed">
          <p className="text-gray-500 mb-2">
            Chưa có dữ liệu {activeTab === "students" ? "học sinh" : "liên kết phụ huynh"}.
          </p>
          <p className="text-sm text-gray-400">
            Tính năng quản lý chi tiết sẽ được hoàn thiện trong các bước tiếp theo.
          </p>
        </div>
      </div>
    </RoleShell>
  );
}
