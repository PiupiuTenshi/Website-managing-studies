"use client";

import { useEffect, useState } from "react";
import { useParams } from "next/navigation";
import { getAssignment } from "@/features/assignments/api"; // Note: this might need adjustment or we just fetch title separately. Actually getAssignment is in assignments/api.ts
import { getManagerSubmissions as getSubs } from "@/features/submissions/api";
import type { ManagerSubmission } from "@/features/submissions/types";
import { RoleShell } from "@/features/auth/components/role-shell";
import Link from "next/link";

export default function ManagerSubmissionsPage() {
  const params = useParams();
  const id = params.id as string;

  const [submissions, setSubmissions] = useState<ManagerSubmission[]>([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    async function load() {
      try {
        const data = await getSubs(id);
        setSubmissions(data);
      } catch (error) {
        console.error(error);
      } finally {
        setLoading(false);
      }
    }
    load();
  }, [id]);

  return (
    <RoleShell role="Manager" title="Danh sách bài nộp">
      <div className="p-8 max-w-6xl mx-auto">
        
        <div className="flex items-center gap-4 mb-8">
          <Link href={`/manager/assignments/${id}`} className="text-gray-500 hover:text-gray-700">
            &larr; Quay lại
          </Link>
          <h1 className="text-2xl font-bold text-gray-900">Danh sách bài nộp</h1>
        </div>

        {loading ? (
          <p className="text-gray-500">Đang tải dữ liệu...</p>
        ) : (
          <div className="bg-white rounded-lg shadow-sm border border-gray-200 overflow-hidden">
            <table className="min-w-full divide-y divide-gray-200">
              <thead className="bg-gray-50">
                <tr>
                  <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Học sinh</th>
                  <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Trạng thái</th>
                  <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Thời gian nộp</th>
                  <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Điểm</th>
                  <th className="px-6 py-3 text-right text-xs font-medium text-gray-500 uppercase tracking-wider">Hành động</th>
                </tr>
              </thead>
              <tbody className="bg-white divide-y divide-gray-200">
                {submissions.map(sub => {
                  return (
                    <tr key={sub.studentId} className="hover:bg-gray-50">
                      <td className="px-6 py-4 whitespace-nowrap">
                        <div className="font-medium text-gray-900">{sub.studentName}</div>
                      </td>
                      <td className="px-6 py-4 whitespace-nowrap">
                        <span className={`px-2 py-1 text-xs font-semibold rounded-full ${
                          sub.status === 'Graded' ? 'bg-blue-100 text-blue-800' :
                          sub.status === 'Submitted' ? 'bg-green-100 text-green-800' :
                          sub.status === 'Late' ? 'bg-yellow-100 text-yellow-800' :
                          sub.status === 'Draft' ? 'bg-gray-100 text-gray-600' :
                          'bg-red-50 text-red-600 border border-red-200'
                        }`}>
                          {sub.status}
                        </span>
                      </td>
                      <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-500">
                        {sub.submittedAt ? new Date(sub.submittedAt).toLocaleString() : '-'}
                      </td>
                      <td className="px-6 py-4 whitespace-nowrap text-sm font-bold text-gray-900">
                        {sub.gradeScore ?? '-'}
                      </td>
                      <td className="px-6 py-4 whitespace-nowrap text-right text-sm font-medium">
                        {(sub.status === 'Submitted' || sub.status === 'Late' || sub.status === 'Graded') ? (
                           <Link href={`/manager/assignments/${id}/submissions/${sub.id}`} className="text-indigo-600 hover:text-indigo-900">
                             {sub.status === 'Graded' ? 'Xem lại điểm' : 'Chấm điểm'}
                           </Link>
                        ) : (
                          <span className="text-gray-400">Chưa nộp</span>
                        )}
                      </td>
                    </tr>
                  );
                })}
                {submissions.length === 0 && (
                  <tr>
                    <td colSpan={5} className="px-6 py-8 text-center text-gray-500 italic">
                      Chưa có học sinh nào được giao bài tập này.
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
