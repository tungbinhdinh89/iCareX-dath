# Functional requirements

Nguồn: `rules.md`, `skill.md`, `sumary.md`, model/form/pages và `fe/tests/data.test.ts`.
Không có specification riêng khác trong repo. Công nghệ cũ trong đề xuất được thay bằng quyết định ASP.NET Core/PostgreSQL.

| FR | Chức năng | Acceptance | Trạng thái backend |
|---|---|---|---|
| FR-01 | Register/login/me/logout | Email unique, Identity password hash, settings mặc định, thu hồi phiên server | Auth controller đã triển khai; host tests và DB smoke cần chạy theo môi trường |
| FR-02 | Xe CRUD | Hai loại, plate unique theo owner, base ODO không vượt entry, delete cascade | Entity/mapping, một phần rules |
| FR-03 | Archive | Giữ lịch sử, chặn mọi ghi; không restore; báo cáo giữ, nhắc hạn bỏ | Property/domain guard, chưa API |
| FR-04 | Fuel/service/ODO | Một timeline ODO, ngày VN không tương lai, cost/liters đúng loại | Entity/constraints/domain rules |
| FR-05 | Kế hoạch | Cả ngày/km và chu kỳ dương; OR; đúng hạn là soon | Entity/rules, chưa API |
| FR-06 | Hoàn tất | Một service có planId, chi phí một lần, sửa/xóa tự tính lại hạn | Domain calculation, chưa API |
| FR-07 | Dashboard/history/filter | Owner scoped; ODO max(base, entries); không suy GPS | Chưa query API |
| FR-08 | Báo cáo/CSV | Cùng owner/xe/tháng; fuel+service; chia 0 an toàn; CSV BOM/escape/chống formula | Chưa làm |
| FR-09 | Profile/settings | Name/phone, chưa đổi email; reminder toggle không đổi trạng thái kế hoạch | Entity, chưa API |
| FR-10 | An toàn ghi | Ownership, transaction, revision, idempotency, rollback | Metadata model, chưa orchestration |
| FR-11 | Nền tảng | Build, migration, tooling, health, Scalar | Có code; kết quả ở testing.md |

## Bất biến

- Ngày YYYY-MM-DD theo Asia/Ho_Chi_Minh; timestamp UTC.
- ODO lower=max(base, các ngày trước), upper=min(các ngày sau); bỏ entry đang sửa; cùng ngày được khác ODO.
- Fuel cost>0/liters>0; service cost>0/liters=0; ODO cost=liters=0. Chỉ service có planId cùng xe/owner.
- Không nhận owner/tổng/ODO hiện tại từ client. Xóa plan unlink entry, giữ chi phí. Delete xe active cascade; archive chặn delete.
- Latest service date DESC, odo DESC; cộng tháng clamp cuối tháng. Không cờ completed thay service.
- Overdue days<0 OR km<0; soon khi chưa overdue và days<=thresholdDays OR km<=thresholdKm.
- Báo cáo/search giữ phạm vi trong rules.md; không mô tả search lọc mọi KPI.

## Validation khi thêm API

Giữ form limits: tên 2–60, plate 5–20, năm 1980..năm VN+1, title 2–100,
note<=500, ODO<=9.999.999, cost<=10 tỷ, liters<=500, intervalKm 1..1 triệu,
intervalMonths 1..120, thresholdKm 1..100.000, thresholdDays 1..365.
Phone rỗng hoặc 10 chữ số bắt đầu 0 sau bỏ khoảng trắng.
decimal tối đa 6 số thập phân, reject trước EF, không tự round.
Domain hiện validate entry/plan/precision; vehicle/profile/settings validation đầy đủ thêm với use case.
Không có background reminder, email/SMS/push, restore, import mock hoặc nối frontend trong đợt này.
