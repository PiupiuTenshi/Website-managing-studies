# Git Rules

> [!IMPORTANT]
> Quy quy trình Git Flow chi tiết, cách commit, push và chính sách ẩn các bản release bị lỗi bắt buộc phải tuân theo hướng dẫn tại: [26. Git Flow, Branch, Commit, Push và Quản lý Bản Release](file:///e:/Project/web_assignment_cloudflare_handbook/docs/26-git-flow.md).

## Branch naming

```text
feature/auth-jwt
feature/assignment-authoring
feature/student-submission
feature/email-outbox
fix/submission-upload-error
docs/update-deployment-guide
refactor/grade-service
```

## Commit message (Conventional Commits)

```text
feat: add assignment scheduling
fix: prevent parent from viewing unrelated student grades
docs: update cloudflare deployment guide
refactor: move smtp sending to email outbox worker
test: add grading authorization tests
```

## Before commit/push Checklist

- Build backend pass (`dotnet build`).
- Build frontend pass (`npm run build`).
- No secret, token, or private credentials committed.
- Database migration reviewed.
- Tests pass for touched module.
- Docs updated if rule/flow changed.
- Quy trình kiểm tra bản release: đảm bảo các bản release lỗi được ẩn khỏi người dùng cuối.

## .gitignore
- Not commit folder docs, templates, deployment

