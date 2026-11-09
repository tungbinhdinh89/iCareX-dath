---
name: icarex-access-control
description: Đặc tả, triển khai hoặc kiểm thử auth/session và phân quyền phương tiện iCareX; dùng cho backend, database, frontend và QA khi có account status, membership, invitation, Contributor authorship hoặc ownership transfer.
---

# Access control nghiệp vụ iCareX

Đọc `doc/authentication-authorization.md`, `doc/rules.md` và tài liệu của
vai trò bên dưới trước thay đổi. Đây là skill nghiệp vụ dùng chung, không
phải một hệ thống authentication mới. Phân biệt source hiện tại và thiết kế
đích; chỉ sửa code/DB/remote trong phạm vi user giao.

## Invariant chung

- Reuse Identity/Account cùng Guid, cookie/session DB và CSRF. Đăng ký
  không tự login; account mới EmailConfirmed=false, email verification bắt buộc
  trước khi vào API phương tiện. Đọc runtime contract `doc/.be/access-control.md`.
  Sáu account states là đích: PendingVerification/Active/Suspended/Locked/
  DeletionPending/Deleted; runtime hiện chỉ derive Active/PendingVerification/
  Locked từ Identity, không giả trạng thái chưa có dữ liệu.
- Role Owner/Manager/Contributor/Viewer theo **từng Vehicle**. Identity Admin
  catalog không làm thành Owner mọi xe. Một account có nhiều role khác nhau.
- Mỗi xe đúng một Owner active. Creator bất biến không phải owner hiện tại.
  Owned/Shared suy từ role, không persist IsShared.
- Manager sửa xe và quản lý fuel/maintenance/plan/reminder, chỉ xem members;
  không invite/update/remove members/transfer/archive.
- Contributor tạo fuel/maintenance; sửa/xóa chỉ own CreatedByUserId; reminder
  completion phải own/assigned. Viewer chỉ đọc theo permission matrix.
- Plan permission của Viewer/Contributor, manual ODO và snooze chưa chốt:
  không tự cấp thêm. Assignment không nâng quyền Viewer; Driver không phải creator.
- Backend resolve membership từ DB và kiểm resource thuộc xe; không tin
  actor/role/permission/creator/owner do Client gửi. Permission map trong code,
  không có per-user grant list v1.
- Create Vehicle+Owner; accept+membership; transfer+owner projection+audit phải
  atomic. Revoke giữ creator/lịch sử và có hiệu lực với request sau commit.
- Invitation chỉ Manager/Contributor/Viewer; token entropy cao, hash-only
  storage, single-use, default 7 ngày, email match, GET không tự Accept.
- Transfer cần recipient active xác nhận, không qua CRUD role/OwnerId.
  Partial unique chỉ bảo đảm tối đa một Owner, transaction phải bảo đảm ít nhất một.
- Archived mặc định chỉ đọc+Owner restore, không tạo nghiệp vụ mới; restore
  không phục hồi membership đã revoke. Không xóa cascade dữ liệu lịch sử.
- 401 là phiên; 403 là thiếu operation; 404 cho ngoài phạm vi xe; 409 cho
  version/state conflict; invitation expired 410. Mã chi tiết là contract đích.

## Backend / database

Đọc `doc/.be/README.md`, `architecture.md`, `api-contracts.md`,
`data-model.md`, `doc/schema-database.md#auth-sharing-schema`.

1. Rà EF/snapshot/contract thật. Hiện Vehicle.AccountId và FK ghép owner-only;
   không xóa AccountId/FK trước khi có kế hoạch expand/backfill/validate.
2. Auth principal + account status → resolve xe từ route/resource → active
   membership → permission → own/assigned predicate → archive/version/domain.
   Query filter trước pagination/aggregation, không chỉ khóa UI.
3. Gắn check quyền với transaction/guard: race revoke-vs-write, accept-vs-revoke/
   expiry/archive, hai accept, hai transfer không được bypass bởi check cũ.
4. Giữ .NET 10/PostgreSQL, không copy filtered-index SQL Server/rowversion.
   Review index, FK, retention và transfer uniqueness/at-least-one-owner.
5. Owner/member/creator dùng Account.Id hiện có (cùng Identity user Id); actor
   từ phiên. API DTO ở Shared, errors/interfaces ở Application, EF ở Infrastructure.
6. Notification thuộc recipient; bấm reference vẫn kiểm quyền xe hiện hành.
   Không log raw token/password/cookie hoặc ghi chúng vào audit/outbox.
7. Giữ /auth/me, PUT vehicle và POST archive/restore hiện tại. Không tự đổi
   sang /me, PATCH/DELETE, response envelope/JWT chỉ vì ví dụ đặc tả.

## Frontend

Đọc `doc/user-flow.md`, `doc/guidelines/Guidelines.md` và skill
`.agents/skills/frontend-design/SKILL.md` nếu sửa hiển thị.

- Chờ /auth/me trước render; anonymous → /dang-nhap; lỗi mạng/5xx có retry;
  session restored không có nút Đăng nhập. Không dùng localStorage token.
- Permission/can tập trung theo account+vehicle+resource, từ API; Viewer không
  write; Contributor Edit/Delete own; Manager read members không mutate.
- Garage Owned/Shared và role là projection thật, không mock quyền.
- Invitation trước login giữ handle an toàn; raw secret không vào URL return,
  log/analytics/localStorage; email mismatch/expired/processed có thông báo.
- Cache/query key theo account/vehicle/filter; invalidate khi role/revoke/transfer.
  403 cập nhật quyền, 404 bỏ view xe ngoài quyền; 401/logout xóa cache riêng.
  Không tự gửi lại command cũ sau login hoặc role change.

## Test / QA

Đọc `doc/.be/testing.md#access-test-matrix`. Test policy thuần và PostgreSQL
Testcontainers/integration/browser theo tác động; không dùng NAS phá dữ liệu.

- A Owner xe1 nhưng Viewer xe2; Manager/Contributor/Viewer và non-member.
- Contributor own/other record, driver khác creator, assigned Viewer vẫn bị chặn.
- Role/account status đổi khi cookie còn hiệu lực; 401/403/404 không lẫn nhau.
- Hai accept/transfer; revoke-vs-write; expired link; email mismatch; rollback
  không tạo xe thiếu Owner hay membership/invitation lệch trạng thái.
- Revoke giữ lịch sử; stale URL và query/report không lộ xe; notification chỉ
  đúng recipient; switch account/xe không còn cache quyền cũ.
- Phân biệt unit/stub với authenticated API/DB thật; ghi Actual/Expected/SHA/
  môi trường. Không coi tài liệu thiết kế hoặc mock browser là E2E passed.

## Handoff

Chỉ docs: kiểm link, anchors, permission matrix, thuật ngữ current/target và
skill frontmatter. Code: build/tests/generator/frontend checks phù hợp, update
`doc/.be/changelog.md`. Còn quyết định mở thì ghi lại, không tự mở quyền.
Không tự migrate, deploy, merge hoặc đóng ticket.
