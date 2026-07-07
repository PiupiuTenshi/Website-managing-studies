"use client";

import { useEffect, useState } from "react";
import { useParams, useRouter } from "next/navigation";
import { getManagerSubmissionDetail, gradeSubmission } from "@/features/submissions/api";
import type { Submission } from "@/features/submissions/types";
import { RoleShell } from "@/features/auth/components/role-shell";
import Link from "next/link";

export default function GradeSubmissionPage() {
  const params = useParams();
  const id = params.id as string;
  const subId = params.subId as string;
  const router = useRouter();

  const [submission, setSubmission] = useState<Submission | null>(null);
  const [loading, setLoading] = useState(true);
  const [actionLoading, setActionLoading] = useState(false);
  
  // Grade Form State
  const [gradeScore, setGradeScore] = useState<number | "">("");
  const [feedbackText, setFeedbackText] = useState("");

  useEffect(() => {
    async function load() {
      try {
        const data = await getManagerSubmissionDetail(subId);
        setSubmission(data);
        if (data.gradeScore !== null) setGradeScore(data.gradeScore);
        if (data.feedbackJson?.text) setFeedbackText(data.feedbackJson.text);
      } catch (error) {
        console.error(error);
      } finally {
        setLoading(false);
      }
    }
    load();
  }, [subId]);

  const handleGrade = async (e: React.FormEvent) => {
    e.preventDefault();
    if (gradeScore === "" || gradeScore < 0 || gradeScore > 100) {
      alert("Vui lòng nhập điểm hợp lệ (0-100).");
      return;
    }

    setActionLoading(true);
    try {
      const updated = await gradeSubmission(subId, {
        gradeScore: Number(gradeScore),
        feedbackJson: { text: feedbackText }
      });
      setSubmission(updated);
      alert("Lưu điểm thành công!");
      router.push(`/manager/assignments/${id}/submissions`);
    } catch (err: any) {
      alert("Lỗi: " + err.message);
    } finally {
      setActionLoading(false);
    }
  };

  if (loading) return <RoleShell role="Manager" title="Loading"><div className="p-8">Loading...</div></RoleShell>;
  if (!submission) return <RoleShell role="Manager" title="Lỗi"><div className="p-8">Không tìm thấy bài nộp.</div></RoleShell>;

  return (
    <RoleShell role="Manager" title="Chấm bài học sinh">
      <div className="p-8 max-w-6xl mx-auto space-y-8">
        
        <div className="flex items-center justify-between">
          <Link href={`/manager/assignments/${id}/submissions`} className="text-gray-500 hover:text-gray-700">
            &larr; Quay lại danh sách
          </Link>
          <div className="flex items-center gap-3">
             <span className={`px-4 py-2 rounded font-semibold text-sm ${
               submission.status === 'Graded' ? 'bg-blue-100 text-blue-800' :
               submission.status === 'Late' ? 'bg-yellow-100 text-yellow-800' :
               'bg-green-100 text-green-800'
             }`}>
               Trạng thái: {submission.status}
             </span>
          </div>
        </div>

        <div className="grid grid-cols-1 lg:grid-cols-3 gap-8">
          
          {/* Khu vực bài làm của học sinh */}
          <div className="lg:col-span-2 space-y-6">
            <div className="bg-white p-6 rounded-lg shadow-sm border border-gray-200">
              <h2 className="text-xl font-semibold mb-4 text-indigo-700 border-b pb-2">Bài làm của Học sinh</h2>
              
              <div className="space-y-4 mt-4">
                <div>
                  <h3 className="text-sm font-medium text-gray-500 uppercase">Nội dung văn bản (Text Answer)</h3>
                  <div className="mt-2 p-4 bg-gray-50 rounded border border-gray-200 text-gray-800 whitespace-pre-line min-h-[200px]">
                    {submission.contentJson?.text || <span className="text-gray-400 italic">Không có nội dung.</span>}
                  </div>
                </div>
                
                <div>
                  <h3 className="text-sm font-medium text-gray-500 uppercase">File đính kèm</h3>
                  <div className="mt-2 p-4 bg-gray-50 rounded border border-dashed border-gray-300 text-gray-500 text-sm italic">
                    (Mockup: Hiển thị danh sách ảnh/file được tải lên từ hệ thống R2)
                  </div>
                </div>
              </div>
            </div>
          </div>

          {/* Khu vực chấm điểm */}
          <div className="space-y-6">
            <div className="bg-white p-6 rounded-lg shadow-sm border border-gray-200">
              <h2 className="text-lg font-semibold mb-4 text-indigo-700 border-b pb-2">Chấm điểm & Nhận xét</h2>
              
              <form onSubmit={handleGrade} className="space-y-4 mt-4">
                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-1">Điểm (Thang 100)</label>
                  <input 
                    type="number" 
                    required 
                    min="0"
                    max="100"
                    step="0.1"
                    value={gradeScore}
                    onChange={e => setGradeScore(e.target.value as any)}
                    className="w-full px-4 py-2 text-xl font-bold text-center border border-gray-300 rounded focus:ring-indigo-500 focus:border-indigo-500" 
                    placeholder="VD: 85"
                  />
                </div>

                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-1">Lời phê / Nhận xét (Feedback)</label>
                  <textarea 
                    rows={6}
                    value={feedbackText}
                    onChange={e => setFeedbackText(e.target.value)}
                    className="w-full px-4 py-2 border border-gray-300 rounded focus:ring-indigo-500 focus:border-indigo-500 text-sm" 
                    placeholder="Nhập nhận xét cho học sinh..."
                  />
                </div>

                <div className="pt-2">
                  <button 
                    type="submit" 
                    disabled={actionLoading}
                    className="w-full py-2 bg-indigo-600 text-white rounded font-medium hover:bg-indigo-700 disabled:opacity-50 transition-colors shadow-sm"
                  >
                    {actionLoading ? "Đang lưu..." : "Lưu Điểm & Nhận xét"}
                  </button>
                </div>
              </form>
            </div>
            
            <div className="bg-white p-6 rounded-lg shadow-sm border border-gray-200 text-sm">
              <h3 className="font-semibold mb-2">Thông tin lịch sử</h3>
              <ul className="space-y-2 text-gray-600">
                <li className="flex justify-between">
                  <span>Nộp lúc:</span>
                  <span>{submission.submittedAt ? new Date(submission.submittedAt).toLocaleString() : '-'}</span>
                </li>
                <li className="flex justify-between">
                  <span>Lần chấm cuối:</span>
                  <span>{submission.gradedAt ? new Date(submission.gradedAt).toLocaleString() : 'Chưa chấm'}</span>
                </li>
              </ul>
            </div>

          </div>
        </div>
      </div>
    </RoleShell>
  );
}
