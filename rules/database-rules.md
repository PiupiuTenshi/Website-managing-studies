# Database Rules

## Naming

- Table dùng snake_case số nhiều: `assignments`, `submissions`.
- Column dùng snake_case: `created_at`, `student_id`.
- Primary key: `id uuid`.
- Foreign key: `<entity>_id`.

## Required columns

Các bảng nghiệp vụ nên có:

```sql
id uuid primary key,
created_at timestamptz not null,
updated_at timestamptz not null,
deleted_at timestamptz null
```

## Soft delete

- Không hard delete dữ liệu nghiệp vụ quan trọng.
- Query mặc định phải filter `deleted_at is null`.
- Khi khóa tài khoản, không xóa submission/grade/log.

## Index

Index bắt buộc cho:

- `assignment_id`
- `student_id`
- `class_id`
- `created_at`
- `status`
- `due_at`

## Migration

- Migration phải review trước production.
- Không drop column/table production nếu chưa backup.
- Không đổi kiểu dữ liệu lớn nếu chưa có plan migrate.
