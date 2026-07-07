# Frontend Next.js Rules

## Component rules

- Component UI không chứa business logic dài.
- Tách form validation schema ra file riêng.
- API call đặt trong `features/<module>/api.ts`.
- Type đặt trong `features/<module>/types.ts` hoặc shared contracts.
- Không để token nhạy cảm trong localStorage nếu kiến trúc có lựa chọn tốt hơn.

## UX rules

- Upload có progress.
- Autosave có trạng thái rõ: Saving/Saved/Failed.
- Nộp bài phải confirm.
- Khi token hết hạn, redirect/login rõ ràng.
- Lỗi validation hiện cạnh field.
- Dashboard dùng empty state thân thiện.

## State management

- Server state dùng TanStack Query/SWR.
- Form state dùng React Hook Form.
- Global auth/user state tối giản.
- Không nhồi mọi thứ vào một context lớn.
