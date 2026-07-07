# Architecture Rules

## Backend

- Dùng Clean Architecture nhẹ.
- Không over-engineer CQRS nếu MVP nhỏ, nhưng vẫn tách use case rõ.
- Không để controller gọi DB trực tiếp.
- Không để email/AI/storage logic nằm trong controller.

## Frontend

- Tổ chức theo feature module.
- Component nhỏ, dễ test.
- Không trộn UI của Admin/Student/Parent trong một component lớn nếu logic khác nhau.

## Integration

- SMTP qua interface.
- AI qua interface.
- Storage qua interface.
- Realtime qua abstraction nếu có thể.

## Background jobs

- Reminder/email/schedule phải chạy idempotent.
- Có retry limit.
- Có dead-letter/failure state.
