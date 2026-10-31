# Flow code tính năng

Các sơ đồ backend dưới đây phân biệt phần đã triển khai với phần thiết kế.
Domain ODO/plan đã có code; nhóm auth/session đã có HTTP endpoints, business
HTTP endpoints còn lại chưa triển khai.
Nguồn frontend hiện tại: `fe/src/store.tsx`, `data.ts`, `Dialogs.tsx`, `pages.tsx`.

## Hiện tại và đích

```mermaid
flowchart LR
    Form[Dialogs hoặc pages] --> Save[useGarage.save callback]
    Save --> Mock[api.update copy Garage trong bộ nhớ]
    Mock --> Cache[TanStack Query cache theo account]
```

Hiện auth gọi API thật qua `/api/v1/auth`; `api.get/update` garage vẫn là mock
preview trong bộ nhớ và không được coi là persistence database.
Đích sau này: thay callback bằng DTO JSON qua adapter HTTP, giữ useGarage và query cache; không gửi/eval callback hoặc upload Garage để overwrite.

## Pipeline ghi chung

```mermaid
sequenceDiagram
    actor User
    participant HTTP as Controller
    participant UC as Application use case
    participant Rules as Domain rules
    participant DB as PostgreSQL
    User->>HTTP: Cookie + CSRF + DTO + If-Match
    HTTP->>UC: Owner đáng tin cậy + command
    UC->>DB: Begin transaction; lock Account
    UC->>DB: Replay idempotency nếu có; check revision
    UC->>DB: Load target theo owner và quan hệ
    UC->>Rules: Archive + input + bất biến
    alt Sai rule, stale revision hoặc khác owner
        UC->>DB: Rollback
        UC-->>HTTP: Lỗi có code
    else Hợp lệ
        UC->>DB: Persist; tăng revision; tạo snapshot/ETag
        UC->>DB: Lưu idempotency response trong cùng transaction
        UC->>DB: Commit
        UC-->>HTTP: Garage + ETag
    end
    HTTP-->>User: JSON hoặc lỗi tiếng Việt
```

Đọc snapshot phải nhất quán revision/data. Mọi admin/import/job tương lai phải đi qua cùng rules.
Rollback toàn bộ khi bất kỳ bước nào lỗi; không xử lý ODO bằng snapshot stale ngoài transaction.

## FR-01: Tài khoản và phiên

Đã triển khai `GET /api/v1/auth/csrf`, register, login, me và logout. Register
tạo Identity user, Account và UserSettings atomically nhưng không tự đăng nhập.
Login tạo AuthSession 8 giờ; cookie HttpOnly được kiểm tra session server-side
ở mỗi request. Logout kiểm tra CSRF, revoke session và xóa cookie. Rate limit,
persistent key ring cho multi-instance, đổi/reset password vẫn chưa triển khai.

```mermaid
flowchart TD
    Register[Register DTO + CSRF] --> Validate[Validate email/password/name/phone]
    Validate --> Tx[Transaction tạo Identity user + Account + Settings]
    Tx --> Registered[201 Account; không tự login]
    Login[Login DTO + CSRF] --> Identity[Identity verify password + lockout]
    Identity --> Session[Protect ticket; lưu AuthSession]
    Session --> Cookie[HttpOnly cookie chứa session key]
    Cookie --> Me[Me: kiểm tra expiry/revocation rồi load Account]
    Logout[Logout + CSRF] --> Revoke[Revoke session + xóa cookie]
```

Email trùng=409; thông báo login sai chung; không log password/ticket. Logout không xóa nghiệp vụ.
FE phải xóa cache cá nhân khi logout/401; không đưa dữ liệu account thật vào public preview.

## FR-02/03: Xe và archive

```mermaid
flowchart TD
    Command[Create hoặc update xe] --> Plate[Trim, uppercase hiển thị; normalize plate]
    Plate --> Validate[Validate form + base ODO với mọi entry]
    Validate --> Unique[DB unique owner + plate]
    Unique --> Save[Lưu trong pipeline transaction]
    Delete[Delete xe active] --> Cascade[Xóa entries + plans cùng transaction]
    Archive[Archive xe active] --> Mark[Đặt ArchivedAtUtc; giữ dữ liệu]
    Mark --> Guard[Mọi ghi xe/entry/plan sau này bị chặn]
```

Khác owner=404; trùng plate kể cả archive=409; archive=403 khi cố ghi. Không restore.
Xóa plan của xe archive cũng bị chặn, không chỉ ẩn nút frontend.

## FR-04: Entry CRUD và ODO

```mermaid
flowchart TD
    Entry[Create hoặc edit entry] --> Relations[Load owner/xe active; plan nếu có phải cùng xe]
    Relations --> Input[Ngày thật không tương lai; cost/liters theo kind]
    Input --> Exclude[Bỏ chính entry đang sửa khỏi history]
    Exclude --> Bounds[lower max base và ngày trước; upper min ngày sau]
    Bounds --> Check{ODO nằm trong khoảng?}
    Check -- Không --> Reject[422 ODO_OUT_OF_SEQUENCE]
    Check -- Có --> Persist[Lưu và dựng snapshot mới]
    Delete[Xóa entry] --> Recompute[Tính lại current ODO, tổng chi, plan]
    Persist --> Recompute
```

Cùng ngày được khác ODO, không dùng thứ tự nhập. Sửa/xóa lớn nhất có thể giảm ODO hiện tại.
Ngày nghiệp vụ từ clock VN; entryId không được dùng để bỏ qua entry khác owner/xe.
POST dùng idempotency để retry không nhân chi phí; việc kiểm tra plan phải lặp lại ở server.

## FR-05/06: Kế hoạch và hoàn tất

```mermaid
flowchart TD
    Plan[Plan CRUD] --> Validation[Đủ ngày/km và chu kỳ dương; xe active]
    Complete[Hoàn tất trên UI] --> Service[POST một service có cost và planId]
    Service --> EntryRules[Chung pipeline ghi entry, không khoản chi thứ hai]
    Validation --> Read[Tính trạng thái khi đọc]
    EntryRules --> Read
    EditDelete[Sửa/xóa service liên kết] --> Read
    Read --> Latest{Còn service liên kết?}
    Latest -- Có --> Next[Latest date DESC odo DESC + chu kỳ hiện tại]
    Latest -- Không --> Initial[Hạn đầu trên plan]
    Next --> Status[OR ngày/km; bằng 0 là soon]
    Initial --> Status
    DeletePlan[Xóa plan] --> Unlink[Unlink entries rồi delete plan; giữ chi phí]
```

Cộng tháng clamp cuối tháng; sửa interval tính lại từ latest service với interval mới.
Tắt reminders không đổi trạng thái; nhắc hạn bỏ xe archive. Không scheduler/email khi web đóng.

## FR-07/08: Dashboard, lịch sử, báo cáo/CSV

```mermaid
flowchart TD
    Read[Garage snapshot theo owner] --> Filter[Chọn xe/tháng/loại trang]
    Filter --> Totals[Cost fuel + service; liters; recorded distance]
    Filter --> Search[Chuỗi tìm kiếm chỉ lọc bảng theo hiện trạng]
    Totals --> KPI[KPI; bảo vệ chia 0]
    Totals --> CSV[CSV toàn bộ bộ lọc; BOM/quote/formula protection]
    Read --> Trend[Biểu đồ 6 tháng có nhãn phạm vi riêng]
```

Quãng đường kỳ = max ODO trong kỳ - max ODO trước kỳ/base, min 0; không entry trong kỳ=0.
Đơn giá nhiên liệu=tổng tiền fuel/tổng lít, không lít/100km. Archive giữ lịch sử cho báo cáo.
Nếu phân trang server về sau, CSV/tổng không được chỉ tính trang đang hiển thị.

## FR-09: Profile/settings

PATCH profile allowlist name/phone, email bất biến; PATCH settings validate thresholds, không xóa kế hoạch.
Cả hai dùng owner/revision transaction như pipeline chung; thành công trả snapshot mới.
Không thay profile email bằng giá trị client để suy owner. Invalid input=422, stale=412.

## Thứ tự triển khai tiếp

Auth/session/CSRF → Garage snapshot/revision pipeline → xe/archive → entry/idempotency → plan → profile/settings → query/report helpers → HTTP adapter frontend.
Mỗi bước cập nhật FR, contract/flow và test trong cùng thay đổi; không cần refactor toàn bộ frontend trước khi backend sẵn sàng.
