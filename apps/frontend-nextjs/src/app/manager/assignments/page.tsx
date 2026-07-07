"use client";

import { useEffect, useState } from "react";
import { getAssignments } from "@/features/assignments/api";
import type { Assignment } from "@/features/assignments/types";
import { RoleShell } from "@/features/auth/components/role-shell";
import Link from "next/link";

export default function ManagerAssignmentsPage() {
  const [assignments, setAssignments] = useState<Assignment[]>([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    async function load() {
      try {
        const data = await getAssignments();
        setAssignments(data);
      } catch (error) {
        console.error("Failed to load assignments", error);
      } finally {
        setLoading(false);
      }
    }
    load();
  }, []);

  return (
    <RoleShell role="Manager" title="Quản lý Bài tập">
      <div className="p-8 max-w-6xl mx-auto">
        <div className="flex justify-between items-center mb-8">
          <div>
            <h1 className="text-3xl font-bold text-gray-900">Danh sách bài tập</h1>
            <p className="text-gray-500 mt-2">Quản lý và soạn thảo các bài tập, kiểm tra</p>
          </div>
          <Link
            href="/manager/assignments/create"
            className="px-4 py-2 bg-indigo-600 text-white rounded shadow-sm hover:bg-indigo-700 font-medium transition-colors"
          >
            + Soạn bài mới
          </Link>
        </div>

        {loading ? (
          <p className="text-gray-500">Đang tải dữ liệu...</p>
        ) : (
          <div className="bg-white rounded-lg shadow-sm border border-gray-200 overflow-hidden">
            <table className="min-w-full divide-y divide-gray-200">
              <thead className="bg-gray-50">
                <tr>
                  <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Tiêu đề</th>
                  <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Trạng thái</th>
                  <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Hạn nộp</th>
                  <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Ngày tạo</th>
                  <th className="px-6 py-3 text-right text-xs font-medium text-gray-500 uppercase tracking-wider">Hành động</th>
                </tr>
              </thead>
              <tbody className="bg-white divide-y divide-gray-200">
                {assignments.map(a => (
                  <tr key={a.id} className="hover:bg-gray-50">
                    <td className="px-6 py-4 whitespace-nowrap">
                      <div className="font-medium text-gray-900">{a.title}</div>
                      <div className="text-sm text-gray-500 max-w-xs truncate">{a.description}</div>
                    </td>
                    <td className="px-6 py-4 whitespace-nowrap">
                      <span className={`px-2 inline-flex text-xs leading-5 font-semibold rounded-full ${
                        a.status === 'Published' ? 'bg-green-100 text-green-800' :
                        a.status === 'Draft' ? 'bg-yellow-100 text-yellow-800' :
                        'bg-gray-100 text-gray-800'
                      }`}>
                        {a.status}
                      </span>
                    </td>
                    <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-500">
                      {a.deadlineAt ? new Date(a.deadlineAt).toLocaleString() : 'Không có hạn'}
                    </td>
                    <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-500">
                      {new Date(a.createdAt).toLocaleDateString()}
                    </td>
                    <td className="px-6 py-4 whitespace-nowrap text-right text-sm font-medium">
                      <Link href={`/manager/assignments/${a.id}`} className="text-indigo-600 hover:text-indigo-900">
                        Sửa
                      </Link>
                    </td>
                  </tr>
                ))}
                {assignments.length === 0 && (
                  <tr>
                    <td colSpan={5} className="px-6 py-8 text-center text-gray-500 italic">
                      Chưa có bài tập nào.
                    </td>
                  </tr>
                )}
              </tbody>
            </table>
          </div>
        )}
      </div>
    </RoleShell>
  );
}
