# Sổ tay chuẩn hóa project website giao bài từ xa

Bộ tài liệu này là blueprint nghiêm túc cho dự án **web giao bài tập từ xa** dùng:

- Backend: ASP.NET Core / .NET Web API
- Frontend: Next.js
- Database: Supabase PostgreSQL
- File storage: ưu tiên Supabase Storage hoặc Cloudflare R2 nếu muốn tối ưu CDN
- Deploy: Next.js trên Cloudflare Pages/Workers; .NET backend chạy trên môi trường hỗ trợ .NET và đặt sau Cloudflare DNS/WAF/Tunnel
- Auth: JWT + refresh token rotation + session tracking
- Roles: Admin, Manager, Student, Parent
- AI: provider dạng pluggable, ưu tiên free-tier hoặc local model khi có thể

> Lưu ý pháp lý: phần học liệu/sách chỉ thiết kế theo hướng nguồn hợp pháp, tài liệu có giấy phép, tài liệu giáo viên tự tải lên hoặc nguồn công khai được phép sử dụng. Không dùng tài liệu này để crawl, sao chép hoặc phát tán sách có bản quyền trái phép.

## Cấu trúc gói

```text
web-assignment-platform/
├── README.md
├── CHANGELOG.md
├── .gitignore
├── .editorconfig
├── docs/
├── rules/
├── templates/
├── database/
├── deployment/
├── parent-pdf-templates/
├── apps/
│   ├── backend-dotnet/
│   └── frontend-nextjs/
└── packages/
    └── shared-contracts/
```

## Cách dùng nhanh

1. Đọc `docs/00-project-brief.md` để hiểu mục tiêu.
2. Đọc `docs/01-requirements.md` và `docs/02-scope-and-legal-boundaries.md` trước khi code.
3. Bắt AI agent hoặc teammate đọc toàn bộ thư mục `rules/` trước khi sửa code.
4. Dùng `database/schema-v1.sql` làm bản nháp database ban đầu.
5. Dùng `deployment/` để triển khai frontend lên Cloudflare và backend sau Cloudflare.
6. Dùng các mẫu PDF trong `parent-pdf-templates/` để gửi phụ huynh.

## Thứ tự triển khai khuyên dùng

1. Auth + role + soft delete account.
2. Assignment CRUD + lớp/khối/môn học.
3. Submission bằng text, ảnh, docx.
4. SMTP email thông báo cho giáo viên/manager.
5. Chấm điểm thang 100 + comment bài làm.
6. Reminder tự động + cảnh báo trễ hạn.
7. Realtime chat.
8. Dashboard điểm, thời gian làm bài, log hoạt động.
9. Video bài giảng.
10. Kho học liệu hợp pháp + trình xem từng trang.
11. AI hỗ trợ soạn bài, gợi ý chấm, giải thích lỗi.
