# Project Rules

## Ưu tiên của project

1. Đúng nghiệp vụ giáo dục.
2. Bảo mật tài khoản, bài nộp, điểm số.
3. UX dễ hiểu cho học sinh/phụ huynh.
4. Code dễ bảo trì.
5. Có log/audit cho thao tác quan trọng.
6. Tài liệu/học liệu hợp pháp.

## General rules

- Không hard delete tài khoản, bài tập, bài nộp, điểm.
- Không gửi email trực tiếp trong transaction chính.
- Không để AI tự chốt điểm nếu chưa cấu hình rõ.
- Không import/crawl tài liệu không có quyền sử dụng.
- Không để frontend quyết định quyền truy cập dữ liệu.
- Không để error message lộ stack trace/token/connection string.

## Definition of Done

Một feature chỉ được xem là xong khi:

- Có validation.
- Có authorization.
- Có error message rõ.
- Có log/audit nếu là thao tác quan trọng.
- Có test happy path và error path.
- Có cập nhật docs nếu thay đổi nghiệp vụ.
