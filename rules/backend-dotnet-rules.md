# Backend .NET Rules

## Layer rules

- WebApi không chứa business logic phức tạp.
- Application chứa use case/service/validation.
- Domain chứa entity/rule cốt lõi.
- Infrastructure chứa DB/storage/email/AI implementation.
- Worker xử lý background job.

## Coding rules

- Dùng async/await cho I/O.
- Dùng cancellation token cho API/service quan trọng.
- Không catch exception rồi nuốt lỗi.
- Không return entity trực tiếp ra API nếu entity có field nhạy cảm.
- Không log password/token/raw refresh token.
- Mọi endpoint thay đổi dữ liệu cần biết user hiện tại.

## Validation

- Validate request bằng FluentValidation hoặc tương đương.
- Validate file type/size server-side.
- Validate điểm trong [0,100].
- Validate due_at > publish_at nếu có.

## Auth

- Dùng `[Authorize]` mặc định.
- Dùng policy/requirement cho quyền phức tạp.
- Check ownership/scope trong application service.
- Refresh token phải hash.
