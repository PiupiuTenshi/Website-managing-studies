"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";
import { createAssignment } from "@/features/assignments/api";
import { RoleShell } from "@/features/auth/components/role-shell";
import Link from "next/link";

export default function CreateAssignmentPage() {
  const router = useRouter();
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState("");
  
  // Basic form state
  const [title, setTitle] = useState("");
  const [description, setDescription] = useState("");
  const [deadlineAt, setDeadlineAt] = useState("");
  const [allowLate, setAllowLate] = useState(false);

  // In MVP, we use a dummy subjectId since subject management is partial.
  // In a real flow, you would select this from a dropdown populated by getSubjects().
  const MOCK_SUBJECT_ID = "00000000-0000-0000-0000-000000000000";

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setLoading(true);
    setError("");

    try {
      const result = await createAssignment({
        subjectId: MOCK_SUBJECT_ID,
        title,
        description,
        deadlineAt: deadlineAt ? new Date(deadlineAt).toISOString() : null,
        allowLateSubmission: allowLate,
        contentJson: { type: "doc", content: [{ type: "paragraph", content: [{ type: "text", text: "Nội dung bài tập (MVP placeholder)" }] }] }
      });
      
      router.push(`/manager/assignments/${result.id}`);
    } catch (err: any) {
      setError(err.message || "Đã xảy ra lỗi khi tạo bài tập.");
      setLoading(false);
    }
  };

  return (
    <RoleShell role="Manager" title="Soạn Bài Tập Mới">
      <div className="p-8 max-w-4xl mx-auto">
        <div className="mb-6 flex items-center gap-4">
          <Link href="/manager/assignments" className="text-gray-500 hover:text-gray-700">
            &larr; Quay lại
          </Link>
          <h1 className="text-2xl font-bold">Tạo bài tập (Draft)</h1>
        </div>

        {error && (
          <div className="bg-red-50 text-red-600 p-4 rounded mb-6 border border-red-200">
            {error}
          </div>
        )}

        <div className="bg-white rounded-lg shadow-sm border border-gray-200 overflow-hidden">
          <form onSubmit={handleSubmit} className="p-6 space-y-6">
            
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">Tiêu đề bài tập <span className="text-red-500">*</span></label>
              <input 
                type="text" 
                required 
                value={title}
                onChange={e => setTitle(e.target.value)}
                className="w-full px-4 py-2 border border-gray-300 rounded focus:ring-indigo-500 focus:border-indigo-500" 
                placeholder="VD: Bài kiểm tra 15 phút Toán đại số"
              />
            </div>

            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">Mô tả (Hướng dẫn học sinh)</label>
              <textarea 
                rows={3}
                value={description}
                onChange={e => setDescription(e.target.value)}
                className="w-full px-4 py-2 border border-gray-300 rounded focus:ring-indigo-500 focus:border-indigo-500" 
                placeholder="Nhập hướng dẫn làm bài..."
              />
            </div>

            <div className="p-6 bg-gray-50 rounded-lg border border-dashed border-gray-300">
              <h3 className="text-sm font-medium text-gray-700 mb-2">Nội dung bài tập (Editor - Mockup)</h3>
              <p className="text-xs text-gray-500 mb-4">
                (Phần này sẽ là Rich Text Editor / Markdown / Drawing Canvas. Trong Phase 3 MVP, chúng tôi dựng UI placeholder. Dữ liệu thực tế được lưu vào content_json field dạng JSONB).
              </p>
              <div className="h-40 bg-white border border-gray-200 rounded flex items-center justify-center text-gray-400">
                [Rich Text Editor Area]
              </div>
            </div>

            <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">Hạn nộp bài (Deadline)</label>
                <input 
                  type="datetime-local" 
                  value={deadlineAt}
                  onChange={e => setDeadlineAt(e.target.value)}
                  className="w-full px-4 py-2 border border-gray-300 rounded focus:ring-indigo-500 focus:border-indigo-500" 
                />
              </div>
              <div className="flex items-center h-full pt-6">
                <label className="flex items-center space-x-3">
                  <input 
                    type="checkbox" 
                    checked={allowLate}
                    onChange={e => setAllowLate(e.target.checked)}
                    className="h-5 w-5 text-indigo-600 rounded focus:ring-indigo-500 border-gray-300"
                  />
                  <span className="text-sm font-medium text-gray-700">Cho phép nộp muộn</span>
                </label>
              </div>
            </div>

            <div className="pt-4 border-t border-gray-200 flex justify-end gap-3">
              <Link href="/manager/assignments" className="px-6 py-2 border border-gray-300 text-gray-700 rounded hover:bg-gray-50">
                Hủy
              </Link>
              <button 
                type="submit" 
                disabled={loading}
                className="px-6 py-2 bg-indigo-600 text-white rounded hover:bg-indigo-700 disabled:opacity-50"
              >
                {loading ? "Đang tạo..." : "Lưu bản nháp (Draft)"}
              </button>
            </div>
          </form>
        </div>
      </div>
    </RoleShell>
  );
}
