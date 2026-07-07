# Security Rules

## Auth/token

- Access token ngắn hạn.
- Refresh token rotation.
- Session revoke khi logout/lock account.
- Token signing key nằm trong secret manager/env, không commit.

## Authorization

- Role check + ownership check.
- Parent chỉ xem dữ liệu của student được link.
- Student chỉ xem assignment/resource được giao/quyền.
- Manager chỉ thao tác trong scope được phân công.

## File security

- Validate extension + MIME + magic bytes nếu có thể.
- Limit file size theo loại.
- Không public file nhạy cảm.
- Signed URL ngắn hạn cho file riêng tư.
- Không execute file upload.

## AI security

- Mask dữ liệu cá nhân nếu không cần.
- Không gửi full database/context không cần thiết.
- Rate limit endpoint AI.
- Ghi rõ AI chỉ là gợi ý.

## Privacy

- Điểm, bài nộp, comment là dữ liệu nhạy cảm.
- Audit ai xem/sửa điểm.
- Không gửi thông tin nhạy cảm cho phụ huynh không được link.
