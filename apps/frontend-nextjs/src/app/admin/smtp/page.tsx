"use client";

import { useState } from "react";
import { testSmtp } from "@/features/settings/api";
import { RoleShell } from "@/features/auth/components/role-shell";

export default function AdminSmtpPage() {
  const [toEmail, setToEmail] = useState("");
  const [toName, setToName] = useState("");
  const [subject, setSubject] = useState("Test Email from Remote Assignment Platform");
  const [bodyHtml, setBodyHtml] = useState("<p>This is a <b>test email</b> to verify SMTP configuration.</p>");
  
  const [loading, setLoading] = useState(false);
  const [resultMessage, setResultMessage] = useState<{ type: 'success' | 'error', text: string } | null>(null);

  const handleTest = async (e: React.FormEvent) => {
    e.preventDefault();
    setLoading(true);
    setResultMessage(null);

    try {
      await testSmtp({ toEmail, toName, subject, bodyHtml });
      setResultMessage({ type: 'success', text: 'Gửi email test thành công! Vui lòng kiểm tra hộp thư.' });
    } catch (err: any) {
      setResultMessage({ type: 'error', text: 'Gửi email thất bại: ' + err.message });
    } finally {
      setLoading(false);
    }
  };

  return (
    <RoleShell role="Admin" title="SMTP Config & Test">
      <div className="p-8 max-w-2xl mx-auto">
        <h1 className="text-2xl font-bold text-gray-900 mb-6">Cấu hình & Test SMTP</h1>
        
        <div className="bg-white p-6 rounded-lg shadow-sm border border-gray-200">
          <p className="text-gray-600 mb-6 text-sm">
            Hệ thống đang sử dụng cấu hình SMTP từ file <code>appsettings.json</code>. 
            Bạn có thể dùng form dưới đây để gửi một email thử nghiệm (Test Email) nhằm đảm bảo cấu hình kết nối đang hoạt động tốt.
          </p>

          {resultMessage && (
            <div className={`p-4 rounded-md mb-6 ${resultMessage.type === 'success' ? 'bg-green-50 text-green-800 border border-green-200' : 'bg-red-50 text-red-800 border border-red-200'}`}>
              {resultMessage.text}
            </div>
          )}

          <form onSubmit={handleTest} className="space-y-4">
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">Email Người nhận (To)</label>
              <input 
                type="email" 
                required 
                value={toEmail}
                onChange={e => setToEmail(e.target.value)}
                className="w-full px-4 py-2 border border-gray-300 rounded focus:ring-indigo-500 focus:border-indigo-500" 
                placeholder="VD: user@example.com"
              />
            </div>
            
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">Tên Người nhận (Tùy chọn)</label>
              <input 
                type="text" 
                value={toName}
                onChange={e => setToName(e.target.value)}
                className="w-full px-4 py-2 border border-gray-300 rounded focus:ring-indigo-500 focus:border-indigo-500" 
                placeholder="VD: Nguyễn Văn A"
              />
            </div>

            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">Tiêu đề (Subject)</label>
              <input 
                type="text" 
                required
                value={subject}
                onChange={e => setSubject(e.target.value)}
                className="w-full px-4 py-2 border border-gray-300 rounded focus:ring-indigo-500 focus:border-indigo-500" 
              />
            </div>

            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">Nội dung HTML (Body)</label>
              <textarea 
                rows={4}
                required
                value={bodyHtml}
                onChange={e => setBodyHtml(e.target.value)}
                className="w-full px-4 py-2 border border-gray-300 rounded focus:ring-indigo-500 focus:border-indigo-500 font-mono text-sm" 
              />
            </div>

            <div className="pt-4">
              <button 
                type="submit" 
                disabled={loading}
                className="w-full py-2 bg-indigo-600 text-white rounded font-medium hover:bg-indigo-700 disabled:opacity-50 transition-colors shadow-sm flex justify-center items-center gap-2"
              >
                {loading ? (
                  <>
                    <svg className="animate-spin h-5 w-5 text-white" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24">
                      <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4"></circle>
                      <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
                    </svg>
                    Đang gửi...
                  </>
                ) : "Gửi Test Email"}
              </button>
            </div>
          </form>
        </div>
      </div>
    </RoleShell>
  );
}
