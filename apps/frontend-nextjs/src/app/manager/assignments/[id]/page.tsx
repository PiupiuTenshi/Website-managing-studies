"use client";

import { useEffect, useState } from "react";
import { useParams, useRouter } from "next/navigation";
import { getAssignment, getAssignmentTargets, setAssignmentTargets, publishAssignment, updateAssignment } from "@/features/assignments/api";
import type { Assignment, AssignmentTarget } from "@/features/assignments/types";
import { RoleShell } from "@/features/auth/components/role-shell";
import Link from "next/link";

export default function AssignmentDetailPage() {
  const params = useParams();
  const id = params.id as string;
  const router = useRouter();

  const [assignment, setAssignment] = useState<Assignment | null>(null);
  const [targets, setTargets] = useState<AssignmentTarget[]>([]);
  const [loading, setLoading] = useState(true);
  const [actionLoading, setActionLoading] = useState(false);
  const [error, setError] = useState("");

  const [targetType, setTargetType] = useState<"ClassRoom" | "Student">("ClassRoom");
  const [targetIdInput, setTargetIdInput] = useState("");

  useEffect(() => {
    async function load() {
      try {
        const [assData, targetData] = await Promise.all([
          getAssignment(id),
          getAssignmentTargets(id)
        ]);
        setAssignment(assData);
        setTargets(targetData);
      } catch (err: any) {
        setError(err.message || "Failed to load assignment");
      } finally {
        setLoading(false);
      }
    }
    load();
  }, [id]);

  const handleAddTarget = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!targetIdInput) return;
    
    setActionLoading(true);
    try {
      const newTargets = [...targets.map(t => ({ targetType: t.targetType, targetId: t.targetId })), { targetType, targetId: targetIdInput }];
      await setAssignmentTargets(id, { targets: newTargets });
      
      const updatedTargets = await getAssignmentTargets(id);
      setTargets(updatedTargets);
      setTargetIdInput("");
    } catch (err: any) {
      alert("Lỗi khi gán mục tiêu: " + err.message);
    } finally {
      setActionLoading(false);
    }
  };

  const handlePublish = async () => {
    if (!confirm("Bạn có chắc chắn muốn Publish bài tập này? Khi Publish, học sinh sẽ nhận được bài và bắt đầu làm bài.")) return;
    setActionLoading(true);
    try {
      const updated = await publishAssignment(id);
      setAssignment(updated);
      alert("Đã Publish bài tập thành công!");
    } catch (err: any) {
      alert("Lỗi: " + err.message);
    } finally {
      setActionLoading(false);
    }
  };

  if (loading) return <RoleShell role="Manager" title="Chi tiết bài tập"><div className="p-8">Loading...</div></RoleShell>;
  if (!assignment) return <RoleShell role="Manager" title="Lỗi"><div className="p-8">Không tìm thấy bài tập hoặc bạn không có quyền xem.</div></RoleShell>;

  return (
    <RoleShell role="Manager" title={assignment.title}>
      <div className="p-8 max-w-5xl mx-auto space-y-8">
        
        <div className="flex items-center justify-between">
          <Link href="/manager/assignments" className="text-gray-500 hover:text-gray-700">
            &larr; Quay lại danh sách
          </Link>
          <div className="flex items-center gap-3">
             <span className={`px-3 py-1 text-sm font-semibold rounded-full ${
                assignment.status === 'Published' ? 'bg-green-100 text-green-800' :
                assignment.status === 'Draft' ? 'bg-yellow-100 text-yellow-800' :
                'bg-gray-100 text-gray-800'
              }`}>
                {assignment.status}
              </span>
              {assignment.status === 'Draft' && (
                <button 
                  onClick={handlePublish}
                  disabled={actionLoading}
                  className="px-4 py-2 bg-green-600 text-white rounded hover:bg-green-700 disabled:opacity-50"
                >
                  Phát hành (Publish)
                </button>
              )}
          </div>
        </div>

        <div className="grid grid-cols-1 lg:grid-cols-3 gap-8">
          
          <div className="lg:col-span-2 space-y-6">
            <div className="bg-white p-6 rounded-lg shadow-sm border border-gray-200">
              <h2 className="text-xl font-semibold mb-4">Chi tiết bài tập</h2>
              <div className="space-y-4">
                <div>
                  <label className="text-xs text-gray-500 font-medium uppercase">Tiêu đề</label>
                  <p className="text-lg font-medium text-gray-900">{assignment.title}</p>
                </div>
                <div>
                  <label className="text-xs text-gray-500 font-medium uppercase">Mô tả / Hướng dẫn</label>
                  <p className="text-gray-700 whitespace-pre-line">{assignment.description || 'Không có mô tả.'}</p>
                </div>
                <div>
                  <label className="text-xs text-gray-500 font-medium uppercase">Nội dung đính kèm</label>
                  <div className="mt-2 p-4 bg-gray-50 rounded border border-gray-200 text-sm font-mono text-gray-600 overflow-x-auto">
                    {JSON.stringify(assignment.contentJson, null, 2)}
                  </div>
                </div>
              </div>
            </div>
          </div>

          <div className="space-y-6">
            <div className="bg-white p-6 rounded-lg shadow-sm border border-gray-200">
              <h2 className="text-lg font-semibold mb-4">Cài đặt nộp bài</h2>
              <ul className="space-y-3 text-sm">
                <li className="flex justify-between py-2 border-b border-gray-100">
                  <span className="text-gray-500">Hạn nộp (Deadline)</span>
                  <span className="font-medium">{assignment.deadlineAt ? new Date(assignment.deadlineAt).toLocaleString() : 'Không có'}</span>
                </li>
                <li className="flex justify-between py-2 border-b border-gray-100">
                  <span className="text-gray-500">Nộp muộn</span>
                  <span className="font-medium">{assignment.allowLateSubmission ? 'Cho phép' : 'Không cho phép'}</span>
                </li>
                <li className="flex justify-between py-2">
                  <span className="text-gray-500">Số lần nộp tối đa</span>
                  <span className="font-medium">{assignment.maxAttempts || 'Không giới hạn'}</span>
                </li>
              </ul>
            </div>

            <div className="bg-white p-6 rounded-lg shadow-sm border border-gray-200">
              <h2 className="text-lg font-semibold mb-4">Giao bài cho (Targets)</h2>
              
              <ul className="mb-4 space-y-2">
                {targets.map(t => (
                  <li key={t.id} className="text-sm p-2 bg-gray-50 rounded border border-gray-100 flex justify-between items-center">
                    <div>
                      <span className="font-medium">{t.targetName}</span>
                      <span className="text-xs text-gray-500 ml-2">({t.targetType})</span>
                    </div>
                  </li>
                ))}
                {targets.length === 0 && <p className="text-sm text-gray-500 italic">Chưa giao cho ai.</p>}
              </ul>

              {assignment.status === 'Draft' && (
                <form onSubmit={handleAddTarget} className="mt-4 pt-4 border-t border-gray-200">
                  <h3 className="text-sm font-medium mb-2">Thêm đối tượng</h3>
                  <div className="flex gap-2 mb-2">
                    <select 
                      value={targetType}
                      onChange={e => setTargetType(e.target.value as any)}
                      className="px-2 py-1 border border-gray-300 rounded text-sm w-1/3"
                    >
                      <option value="ClassRoom">Lớp học</option>
                      <option value="Student">Học sinh</option>
                    </select>
                    <input 
                      type="text" 
                      placeholder="Nhập Target UUID..."
                      value={targetIdInput}
                      onChange={e => setTargetIdInput(e.target.value)}
                      className="px-2 py-1 border border-gray-300 rounded text-sm flex-1"
                      required
                    />
                  </div>
                  <button type="submit" disabled={actionLoading} className="w-full py-1 text-sm bg-indigo-50 text-indigo-700 rounded hover:bg-indigo-100 transition-colors">
                    + Thêm
                  </button>
                </form>
              )}
            </div>
          </div>

        </div>
      </div>
    </RoleShell>
  );
}
