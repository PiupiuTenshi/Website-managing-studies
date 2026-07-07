"use client";

import { useEffect, useState } from "react";
import { useParams, useRouter } from "next/navigation";
import { getStudentAssignments, getStudentSubmission, draftSubmission, submitAssignment } from "@/features/submissions/api";
import type { StudentAssignment, Submission } from "@/features/submissions/types";
import { RoleShell } from "@/features/auth/components/role-shell";
import Link from "next/link";

export default function StudentAssignmentDetailPage() {
  const params = useParams();
  const id = params.id as string;
  const router = useRouter();

  const [assignment, setAssignment] = useState<StudentAssignment | null>(null);
  const [submission, setSubmission] = useState<Submission | null>(null);
  const [loading, setLoading] = useState(true);
  const [actionLoading, setActionLoading] = useState(false);
  
  // Submission Content
  const [textAnswer, setTextAnswer] = useState("");

  useEffect(() => {
    async function load() {
      try {
        // Fetch assignment list to find the details (or we could make a specific API)
        const assignments = await getStudentAssignments();
        const current = assignments.find(a => a.assignmentId === id);
        
        if (current) {
          setAssignment(current);
          const sub = await getStudentSubmission(id);
          setSubmission(sub);
          
          if (sub?.contentJson?.text) {
            setTextAnswer(sub.contentJson.text);
          }
        }
      } catch (err: any) {
        console.error(err);
      } finally {
        setLoading(false);
      }
    }
    load();
  }, [id]);

  const handleSaveDraft = async () => {
    setActionLoading(true);
    try {
      const updated = await draftSubmission(id, { contentJson: { text: textAnswer } });
      setSubmission(updated);
      alert("Đã lưu nháp!");
    } catch (err: any) {
      alert("Lỗi: " + err.message);
    } finally {
      setActionLoading(false);
    }
  };

  const handleSubmit = async () => {
    if (!confirm("Bạn có chắc chắn nộp bài? Không thể sửa sau khi nộp.")) return;
    setActionLoading(true);
    try {
      const updated = await submitAssignment(id, { contentJson: { text: textAnswer } });
      setSubmission(updated);
      alert("Đã nộp bài thành công!");
      router.push("/student/assignments");
    } catch (err: any) {
      alert("Lỗi: " + err.message);
    } finally {
      setActionLoading(false);
    }
  };

  if (loading) return <RoleShell role="Student" title="Loading"><div className="p-8">Loading...</div></RoleShell>;
  if (!assignment) return <RoleShell role="Student" title="Lỗi"><div className="p-8">Không tìm thấy bài tập.</div></RoleShell>;

  const isSubmitted = submission?.status === 'Submitted' || submission?.status === 'Late' || submission?.status === 'Graded';

  return (
    <RoleShell role="Student" title={assignment.title}>
      <div className="p-8 max-w-5xl mx-auto space-y-8">
        
        <div className="flex items-center justify-between">
          <Link href="/student/assignments" className="text-gray-500 hover:text-gray-700">
            &larr; Quay lại danh sách
          </Link>
          <div className="flex items-center gap-3">
             {isSubmitted ? (
               <span className="px-4 py-2 bg-green-100 text-green-800 rounded font-semibold text-sm">
                 Bài của bạn đã được nộp. ({submission.status})
               </span>
             ) : (
               <span className="px-4 py-2 bg-yellow-100 text-yellow-800 rounded font-semibold text-sm">
                 Chưa nộp bài
               </span>
             )}
          </div>
        </div>

        <div className="grid grid-cols-1 lg:grid-cols-3 gap-8">
          
          {/* Đề bài & Hướng dẫn */}
          <div className="lg:col-span-2 space-y-6">
            <div className="bg-white p-6 rounded-lg shadow-sm border border-gray-200">
              <h2 className="text-xl font-semibold mb-4">Chi tiết bài tập</h2>
              <div className="space-y-4">
                <div>
                  <h3 className="text-lg font-medium text-gray-900">{assignment.title}</h3>
                  <p className="text-gray-700 whitespace-pre-line mt-2">{assignment.description || 'Không có mô tả chi tiết.'}</p>
                </div>
              </div>
            </div>

            {/* Khu vực nộp bài */}
            <div className="bg-white p-6 rounded-lg shadow-sm border border-gray-200">
              <h2 className="text-xl font-semibold mb-4">Nộp bài của bạn</h2>
              
              <div className="space-y-4">
                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-2">Nhập nội dung trả lời trực tiếp:</label>
                  <textarea 
                    rows={8}
                    disabled={isSubmitted}
                    value={textAnswer}
                    onChange={e => setTextAnswer(e.target.value)}
                    className="w-full px-4 py-2 border border-gray-300 rounded focus:ring-indigo-500 focus:border-indigo-500 disabled:bg-gray-50 disabled:text-gray-500" 
                    placeholder="Viết câu trả lời của bạn vào đây..."
                  />
                </div>
                
                <div className="p-4 bg-gray-50 rounded border border-dashed border-gray-300">
                  <h3 className="text-sm font-medium text-gray-700 mb-1">Đính kèm File (Upload Ảnh, DOCX, PDF)</h3>
                  <p className="text-xs text-gray-500 mb-2">
                    (Mockup: Chức năng upload file vật lý lên hệ thống Cloudflare R2 sẽ được kích hoạt ở các phase nâng cao).
                  </p>
                  <button disabled className="px-4 py-2 bg-white border border-gray-300 text-gray-500 rounded text-sm cursor-not-allowed">
                    Chọn File...
                  </button>
                </div>
              </div>

              {!isSubmitted && (
                <div className="mt-6 flex justify-end gap-3 pt-4 border-t border-gray-200">
                  <button 
                    onClick={handleSaveDraft}
                    disabled={actionLoading}
                    className="px-6 py-2 border border-gray-300 text-gray-700 rounded hover:bg-gray-50 disabled:opacity-50 font-medium"
                  >
                    {actionLoading ? "Đang lưu..." : "Lưu bản nháp"}
                  </button>
                  <button 
                    onClick={handleSubmit}
                    disabled={actionLoading}
                    className="px-6 py-2 bg-indigo-600 text-white rounded hover:bg-indigo-700 disabled:opacity-50 font-medium"
                  >
                    Nộp bài chính thức
                  </button>
                </div>
              )}
            </div>
          </div>

          {/* Thông tin phụ */}
          <div className="space-y-6">
            <div className="bg-white p-6 rounded-lg shadow-sm border border-gray-200">
              <h2 className="text-lg font-semibold mb-4">Thông tin nộp bài</h2>
              <ul className="space-y-3 text-sm">
                <li className="flex justify-between py-2 border-b border-gray-100">
                  <span className="text-gray-500">Giáo viên</span>
                  <span className="font-medium">{assignment.createdByName}</span>
                </li>
                <li className="flex justify-between py-2 border-b border-gray-100">
                  <span className="text-gray-500">Hạn nộp</span>
                  <span className="font-medium text-red-600">{assignment.deadlineAt ? new Date(assignment.deadlineAt).toLocaleString() : 'Không có'}</span>
                </li>
                <li className="flex justify-between py-2 border-b border-gray-100">
                  <span className="text-gray-500">Cho phép nộp muộn</span>
                  <span className="font-medium">{assignment.allowLateSubmission ? 'Có' : 'Không'}</span>
                </li>
                {isSubmitted && (
                  <>
                    <li className="flex justify-between py-2 border-b border-gray-100">
                      <span className="text-gray-500">Thời gian đã nộp</span>
                      <span className="font-medium text-green-600">{submission?.submittedAt ? new Date(submission.submittedAt).toLocaleString() : ''}</span>
                    </li>
                    {submission?.gradeScore != null && (
                       <li className="flex justify-between py-2 border-b border-gray-100">
                        <span className="text-gray-500">Điểm</span>
                        <span className="font-bold text-blue-600">{submission.gradeScore}</span>
                      </li>
                    )}
                  </>
                )}
              </ul>
            </div>
          </div>

        </div>
      </div>
    </RoleShell>
  );
}
