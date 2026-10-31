# VehicleCare — Quy trình phát triển và lộ trình backend

> Quyết định backend mới: ASP.NET Core 10 + PostgreSQL, Clean Architecture monolith.
> Nền tảng đã được triển khai; contract/flow/trạng thái hiện tại ở [doc/.be/README.md](doc/.be/README.md).
> Phần B dưới đây là đề xuất lịch sử; ưu tiên bộ `.be` khi làm backend.

Tài liệu thao tác cho agent. Đọc [sumary.md](./sumary.md) để biết cấu trúc và
[rules.md](./rules.md) trước khi triển khai.

## Quy trình ticket bắt buộc

Trước mọi thay đổi code thuộc feature, enhancement, fix hoặc bug, tạo/đối chiếu
issue và làm trên branch riêng từ `main`: `feature/<issue>-<slug>`,
`enhancement/<issue>-<slug>`, `fix/<issue>-<slug>` hoặc
`bug/<issue>-<slug>`. Implement theo scope/acceptance criteria của ticket,
chạy kiểm tra, self-review, rồi mở một PR liên kết ticket về `main`.

Agent không được tự approve PR, merge PR, bật auto-merge, bypass protection hay
đóng issue. Sau khi PR sẵn sàng review, phải dừng và chờ chủ repository xác
nhận approve; sau khi merge cũng phải chờ xác nhận riêng trước khi đóng issue.
Nếu phát hiện scope mới, tạo ticket/branch/PR mới thay vì âm thầm mở rộng PR
đang review. Cập nhật tài liệu và changelog khi thay đổi backend hoặc contract.

**Ranh giới:** phần A áp dụng cho frontend hiện tại; phần B là thiết kế đề xuất,
chưa có code/backend/database tương ứng. Không tự cài dependency, tạo secrets,
kết nối dịch vụ hoặc chuyển runtime chỉ vì một công nghệ được nhắc ở đây.

## A. Phát triển frontend hiện tại

### A1. Bước 1 — Xác định phạm vi và đọc đúng nguồn

1. Đọc `AGENTS.md`, `README.md`, `package.json`; kiểm tra `git status --short`.
2. Xác định đây là yêu cầu sửa code, hỏi đáp, review hay chỉ viết tài liệu.
3. Tìm route/component/hàm liên quan trước khi đọc rộng:

| Công việc | Điểm bắt đầu |
| --- | --- |
| Seed, model, ODO, kỳ bảo dưỡng | `src/data.ts`, `tests/data.test.ts` |
| Phiên, cache, thông báo kết quả lưu | `src/store.tsx` |
| Form và xác nhận xóa | `src/Dialogs.tsx` |
| Màn hình, thống kê, CSV, hồ sơ, cài đặt | `src/pages.tsx` |
| Biểu đồ, bảng, filter dùng chung | `src/components.tsx` |
| Route | `src/app/routes.tsx` |
| Menu, header, drawer mobile | `src/Layout.tsx` |
| Màu, font, khoảng cách, responsive | `src/index.css`, `src/theme.ts` |

4. Phân biệt lỗi cần sửa với chức năng thiếu ngoài phạm vi.
5. Nếu đổi UI, nạp skill `make/use-design-system` của môi trường Make trước khi
   triển khai; giữ Astra Make Kit và MUI hiện có. Các tài liệu này không thay thế
   skill bắt buộc của môi trường.

### A2. Bước 2 — Chốt model và nghiệp vụ trước UI

1. Đọc type hiện tại trong `src/data.ts`; không tự đổi enum, đơn vị hay định dạng ngày.
2. Liệt kê tác động đến ODO, chi phí, kế hoạch và báo cáo khi thêm/sửa/xóa.
3. Với field mới, xác định default cho cả hai seed và cách đọc dữ liệu cũ.
4. Ưu tiên hàm nghiệp vụ dùng chung, kiểm thử được; không sao chép công thức vào
   từng màn hình.
5. Viết hoặc bổ sung test cho thay đổi nghiệp vụ trước khi nối vào form.
6. Chỉ tách module khi giúp giảm độ phức tạp thực sự; không tổ chức lại toàn
   ứng dụng cho một sửa đổi nhỏ.

### A3. Bước 3 — Cập nhật mock an toàn

1. Sửa `seed(account)` nếu cần dữ liệu minh họa; bảo đảm hai tài khoản tách biệt.
2. Ngày mẫu tương đối dùng các helper hiện có; trường ngày nghiệp vụ giữ dạng ISO date.
3. Kiểm tra đủ tình huống: có dữ liệu, rỗng, sai đầu vào, quá hạn, sắp hạn.
4. Không thêm `localStorage`, `sessionStorage`, IndexedDB hay lưu phiên tự động.
5. Ghi qua `save()`/`api.update()` hiện có; không sửa trực tiếp object lấy từ query.
6. Quy tắc quan trọng phải được kiểm tra lại trên bản dữ liệu đang được ghi,
   không chỉ trên snapshot lúc mở form.

### A4. Bước 4 — Triển khai form và tương tác

1. Theo mẫu React Hook Form + Zod trong `src/Dialogs.tsx`; profile ở `src/pages.tsx`.
2. Giữ lỗi tiếng Việt cạnh field; lỗi nghiệp vụ hiển thị rõ trong form/toast.
3. Chuyển `dd/MM/yyyy` sang `YYYY-MM-DD` ở biên form, kiểm tra ngày thực tồn tại.
4. Dùng `valueAsNumber` phù hợp; chặn `NaN`, vô cực và giá trị ngoài giới hạn.
5. Dùng `open()` cho hành động cần đăng nhập; không tự tạo phiên giả trong page.
6. Khóa nút khi `pending`; chỉ đóng form khi `save()` trả thành công.
7. Hoàn tất kế hoạch phải mở form `service` mang theo kế hoạch, không chỉ đổi
   một cờ trạng thái hoặc thêm chi phí vào một mảng khác.
8. Nếu thêm màn hình, khai báo route và cập nhật menu phù hợp; giữ 404, loading,
   error, empty state và khả năng dùng bàn phím.

### A5. Bước 5 — Giữ giao diện nhất quán

- Font đã được khai báo trong `src/index.css`: Be Vietnam Pro 400/500/600.
- Dùng token chung cho màu, typography, spacing, border, radius. Token mới phải
  được khai báo ở nguồn CSS chung, không tạo theme cạnh tranh.
- MUI theme đọc token qua `src/theme.ts`; Chart.js đọc token trong `chartColors()`
  ở `src/components.tsx`. Không hard-code một bảng màu riêng cho biểu đồ.
- Giữ ảnh hiện có và giấy phép; không tự thay tài nguyên ngoài phạm vi.
- Kiểm tra layout mobile, tablet, desktop qua preview đang được môi trường chạy.
- Không coi các literal còn tồn tại trong code cũ là lý do rải thêm magic values
  hoặc dọn toàn bộ CSS ngoài yêu cầu.

### A6. Bước 6 — Kiểm tra đúng mức và bàn giao

Các lệnh chuẩn của repository trong `README.md`:

```sh
pnpm format src tests
pnpm exec tsc --noEmit
pnpm test
pnpm build
```

- Thay đổi tài liệu/cục bộ: chỉ kiểm tra liên quan, không tự chạy cả bộ.
- Đổi nghiệp vụ: `pnpm test`; thay đổi rộng về type/tích hợp: chạy thêm
  `pnpm exec tsc --noEmit`, `pnpm build` khi cần.
- `pnpm format src tests` là lệnh **có sửa file**. Chỉ chạy khi phù hợp phạm vi,
  kiểm tra diff sau đó, không ghi đè thay đổi có sẵn của người dùng.
- `pnpm test` hiện chạy `tests/data.test.ts` bằng Node test runner, không phải
  browser test; typecheck hiện chỉ bao gồm `src` và `vite.config.ts`.
- Không có script `lint` hoặc E2E trong `package.json`; không báo đã chạy chúng.
- Không chạy thêm `pnpm dev`, `npm run dev` hoặc server tương đương trong Make.
- Không đọc/poll log mặc định; chỉ dùng log để điều tra lỗi cụ thể.
- Cuối cùng kiểm tra `git diff --check`, liệt kê file đổi và lệnh thực sự đã chạy.
  Không tự commit/push.

## B. Thiết kế backend/database thật — đề xuất chưa triển khai

### B0. Logging backend

Backend ASP.NET Core hiện dùng Serilog qua `Serilog.AspNetCore`,
`Serilog.Sinks.Console` và `Serilog.Sinks.File`. Khi sửa backend, dùng
`ILogger<T>` và structured logging; log request, migration/database operation
và trạng thái auth ở mức phù hợp. Không ghi password, token, cookie, connection
string hoặc request body nhạy cảm. Log được xem ở console khi `dotnet run/watch`
và file rolling theo ngày trong thư mục `logs/`; không commit thư mục này.

### B1. Bước 1 — Chốt lựa chọn triển khai

Trước khi viết backend, xác nhận:

1. Dùng backend độc lập hay backend được tích hợp trong Make. Nếu dùng Payload,
   thực hiện quy trình kết nối backend và nạp các skill Payload tương ứng trước
   khi sửa schema/nội dung; không giả lập server trong Vite.
2. Cơ chế xác thực và chính sách phiên thật; có giữ chế độ demo/preview riêng không.
3. Có triển khai đăng ký trong phiên cho prototype, đăng ký tài khoản thật,
   hay chỉ demo hai tài khoản. Yêu cầu đăng ký cũ chưa được xác nhận loại bỏ.
4. Bổ sung xe lưu trữ chỉ đọc; xác nhận có khôi phục hay không và cách đưa xe lưu
   trữ vào nhắc hạn/bộ lọc/báo cáo.
5. Có cần nhắc nền không. Nếu có, đó là hạng mục riêng với scheduler, kênh gửi,
   consent và cơ chế chống gửi trùng.

Có thể ưu tiên backend TypeScript và database quan hệ như PostgreSQL để thuận
lợi cho ngày, số thập phân, ràng buộc và transaction, nhưng **chưa chọn công nghệ**.
Không đổi frontend sang Next.js hoặc thêm Payload/ORM khi chưa được yêu cầu.

### B2. Bước 2 — Giữ ranh giới kiến trúc

```text
React/MUI
  → useGarage + TanStack Query
  → adapter gọi API bằng JSON
  → xác thực + validation DTO + kiểm tra ownership
  → dịch vụ nghiệp vụ (ODO, hoàn tất kế hoạch, archive)
  → repository/transaction
  → database
```

- Database là nguồn chuẩn; client validation chỉ hỗ trợ trải nghiệm.
- Frontend không truy cập database bằng khóa quản trị hoặc chứa secret.
- Dịch vụ nghiệp vụ phải được dùng cho mọi đường ghi, kể cả admin/import/job;
  nếu dùng CMS, cần access rules/hook tương ứng để không bypass qua admin/API khác.
- Lúc đầu có thể trả toàn bộ `Garage` để giảm thay đổi màn hình; quy mô lớn mới
  tách truy vấn phân trang và báo cáo server.
- Các tên như `src/api/garage.ts`, `src/api/contracts.ts`, `src/domain/` là
  **vị trí có thể thêm**, chưa tồn tại và không bắt buộc tái cấu trúc ngay.

### B3. Bước 3 — Thiết kế dữ liệu lưu bền

Bảng dưới là mô hình logic đề xuất, không phải migration đã chạy. Nếu chọn CMS,
ánh xạ thành collection/field/relationship tương đương và giữ nguyên bất biến.

| Bảng | Trường chính đề xuất | Ánh xạ frontend |
| --- | --- | --- |
| `users` | `id`, `email`, `password_hash` hoặc định danh nhà cung cấp auth, `name`, `phone`, `garage_revision`, timestamps | `Account`, `Garage.profile` |
| `user_settings` | `user_id` unique/FK, `reminders`, `threshold_km`, `threshold_days` | `Garage.settings` |
| `vehicles` | `id`, `user_id`, `name`, `plate`, `plate_normalized`, `type`, `year`, `base_odo`, `image_url`, `archived_at` nullable, timestamps | `Vehicle`; archive cần mở rộng DTO/UI |
| `maintenance_plans` | `id`, `user_id`, `vehicle_id`, `title`, `due_date`, `due_odo`, `interval_km`, `interval_months`, timestamps | `Plan` |
| `entries` | `id`, `user_id`, `vehicle_id`, `kind`, `title`, `date`, `odo`, `cost_vnd`, `liters`, `note`, `plan_id` nullable, timestamps | `Entry` |

Quy ước lưu trữ:

- ID thật do server sinh, chuỗi opaque; không áp UUID validation lên ID mock.
- `date`, `due_date`: kiểu `DATE`; timestamps kiểm toán lưu UTC. Ngày hiện tại
  và kiểm tra tương lai phải tính theo `Asia/Ho_Chi_Minh`.
- VNĐ dùng số nguyên đủ lớn; ODO/lít dùng số thập phân chính xác với độ chính xác
  đã thống nhất, không mặc định ép ODO thành số nguyên vì form hiện chưa yêu cầu.
- DTO hiện dùng JavaScript number. Adapter phải kiểm tra finite, giới hạn và
  độ an toàn khi chuyển `BIGINT`/`NUMERIC`; không chuyển chuỗi tùy tiện bằng ép kiểu.
- Không lưu tổng chi phí, ODO hiện tại, `status` kế hoạch làm nguồn chuẩn thứ hai.
  Nếu cache chúng để tối ưu, phải có cơ chế cập nhật và xây lại từ lịch sử.
- Không cần bảng khoản chi hoặc bảng hoàn tất riêng cho luồng hiện tại:
  `entries.kind = service` với `plan_id` đã biểu diễn lần hoàn tất.

Ràng buộc và chỉ mục:

1. `UNIQUE(user_id, plate_normalized)` để chặn biển số trùng trong cùng tài khoản,
   kể cả cạnh tranh đồng thời; chưa tự miễn trùng cho xe archive.
2. FK và kiểm tra đảm bảo entry/plan/vehicle cùng owner; `plan_id` nếu có phải
   trỏ tới kế hoạch cùng xe, và entry phải là `service`.
3. Dùng composite FK hoặc dịch vụ/trigger tương đương cho quan hệ cùng owner/xe.
   FK đơn chỉ kiểm tra bản ghi tồn tại là chưa đủ.
4. `CHECK`/validation: số hữu hạn và không âm; chi phí fuel/service > 0,
   liters fuel > 0, ODO thuần cost/liters = 0, service liters = 0.
5. Check ngày hợp lệ, không ghi lịch sử tương lai theo ngày Việt Nam; quy tắc
   liên bản ghi và archive được kiểm tra trong transaction.
6. Chỉ mục gợi ý: entries `(user_id, vehicle_id, date, odo)`,
   `(user_id, plan_id, date, odo)`; plans `(user_id, vehicle_id)`.
7. Xóa plan phải giữ entry và bỏ liên kết; xóa xe xóa entries/plans liên quan
   theo hành vi hiện tại. Chọn cascade/thứ tự transaction tránh FK treo.
8. Archive khác delete: giữ toàn bộ lịch sử và chặn ghi lên xe cùng mọi entry/plan
   của nó. Không tự tạo đường khôi phục nếu chưa chốt.

### B4. Bước 4 — Chốt JSON contract tương thích frontend

Các endpoint dưới đây **là đề xuất**, chưa có route HTTP nào trong repo.
Prefix minh họa `/api/v1`; nếu dùng Payload, có thể dùng adapter/custom endpoint
để giữ contract mà không ép React đọc trực tiếp định dạng collection CMS.

#### Xác thực

| Method/route đề xuất | Kết quả |
| --- | --- |
| `POST /api/v1/auth/login` | Nhận email/password, trả `Account`; thiết lập phiên an toàn |
| `GET /api/v1/auth/me` | Trả `Account` của phiên hoặc 401 |
| `POST /api/v1/auth/logout` | Hủy phiên trên server, trả 204 |

Cookie `HttpOnly`, `Secure`, `SameSite` phù hợp là một phương án đề xuất; nếu
chọn cookie phải xử lý CSRF, credentials và CORS theo topology triển khai.
Không tự đưa token vào localStorage hoặc dùng mật khẩu demo cho production.
Endpoint đăng ký chỉ thiết kế sau khi chốt phạm vi.

#### Dữ liệu nghiệp vụ

Phiên bản contract đầu tiên có thể dùng snapshot `Garage`:

- `GET /api/v1/garage` trả trực tiếp object đúng cấu trúc `Garage` và header
  `ETag` của snapshot.
- Các mutation bên dưới nhận JSON, suy owner từ phiên, trả snapshot `Garage`
  mới cùng `ETag`: 201 khi tạo mới, 200 khi sửa/xóa.
- Riêng logout dùng 204, không parse body JSON.
- Header `If-Match` gửi ETag snapshot đang sửa; không khớp trả 412, yêu cầu tải
  lại dữ liệu thay vì âm thầm ghi đè.
- Lưu revision ở metadata phía adapter/query, không thêm tùy tiện vào domain type.

| Method/route đề xuất | Ý nghĩa |
| --- | --- |
| `POST /api/v1/vehicles` | Tạo xe |
| `PATCH /api/v1/vehicles/:id` | Sửa xe, không cho đổi owner |
| `DELETE /api/v1/vehicles/:id` | Xóa xe đang hoạt động và dữ liệu liên quan, có xác nhận UI |
| `POST /api/v1/vehicles/:id/archive` | Chuyển xe hoạt động sang chỉ đọc; cần DTO/UI archive |
| `POST /api/v1/entries` | Tạo fuel/service/odo; service có `planId` hợp lệ là hoàn tất kế hoạch |
| `PATCH /api/v1/entries/:id` | Sửa nội dung hợp lệ; không tự chuyển xe/kind/liên kết nếu UI không hỗ trợ |
| `DELETE /api/v1/entries/:id` | Xóa bản ghi và tính lại dữ liệu suy ra |
| `POST /api/v1/plans` | Tạo kế hoạch |
| `PATCH /api/v1/plans/:id` | Sửa kế hoạch, giữ quan hệ xe nhất quán |
| `DELETE /api/v1/plans/:id` | Xóa kế hoạch, giữ chi phí/lịch sử dịch vụ |
| `PATCH /api/v1/profile` | Chỉ các trường hồ sơ được cho phép; email demo hiện không sửa được |
| `PATCH /api/v1/settings` | Cập nhật nhắc nhở/ngưỡng |

Ví dụ body đề xuất cho `POST /api/v1/entries` để hoàn tất kế hoạch
(ID chỉ minh họa; `date` phải được chọn hợp lệ theo ngày thực tế):

```json
{
  "vehicleId": "vehicle-server-id",
  "kind": "service",
  "title": "Thay dầu động cơ",
  "date": "2026-01-31",
  "odo": 42000,
  "cost": 450000,
  "liters": 0,
  "note": "Bảo dưỡng định kỳ",
  "planId": "plan-server-id"
}
```

Body không nhận `userId`, ODO hiện tại, trạng thái hoàn tất hoặc tổng chi phí
do client tự tính. Một lần ghi service là đủ, không gọi thêm API ghi chi phí.
Nếu sau này thêm endpoint chuyên biệt “complete plan”, endpoint đó cũng phải
gọi cùng dịch vụ ghi entry trong một transaction, không tạo luồng hoàn tất thứ hai.

Lỗi thống nhất, ví dụ:

```json
{
  "error": {
    "code": "ODO_OUT_OF_SEQUENCE",
    "message": "ODO không nhất quán với lịch sử.",
    "fields": {
      "odo": "ODO phải nằm trong khoảng hợp lệ của ngày ghi nhận."
    }
  }
}
```

Mã HTTP đề xuất: 401 chưa xác thực; 403 thao tác bị chặn trên xe archive;
404 không tồn tại/không thuộc owner theo chính sách tránh lộ dữ liệu;
409 trùng biển số hoặc xung đột idempotency; 412 snapshot cũ;
422 dữ liệu không hợp lệ; 500 lỗi nội bộ không lộ chi tiết database/secret.

### B5. Bước 5 — Cài đặt transaction nghiệp vụ

Quy trình tham chiếu cho mỗi mutation:

1. Xác thực phiên; tải bản ghi bằng cả ID và owner. Không tin account ID từ client.
2. Mở transaction; khóa dòng owner để tuần tự hóa ghi và kiểm tra
   `garage_revision`/`If-Match`. Đây là thiết kế đơn giản cho snapshot toàn Garage;
   chỉ chuyển sang khóa theo xe khi đã thiết kế revision tương ứng.
3. Kiểm tra xe còn hoạt động; kiểm tra quan hệ xe/kế hoạch/entry.
4. Validate payload và các bất biến trong `rules.md` trên dữ liệu mới nhất.
   Khi sửa entry phải loại chính entry cũ khỏi tập đối chiếu ODO.
5. Ghi thay đổi; xóa/giữ liên kết đúng chính sách; không ghi thêm khoản chi.
6. Tăng revision; tạo snapshot và ETag nhất quán từ transaction, rồi commit.
   `GET /garage` cũng phải đọc snapshot/revision nhất quán.
7. Lỗi ở bất kỳ bước nào rollback toàn bộ; trả lỗi có cấu trúc.

Với tạo bản ghi, nên hỗ trợ `Idempotency-Key` để retry mạng không sinh hai
dịch vụ/chi phí. Khóa phải theo owner + key, gắn hash payload, lưu kết quả trong
cùng transaction; key cũ với payload khác trả conflict. Khi replay kết quả cũ,
client cần refetch snapshot mới nhất, không ghi đè cache mới bằng snapshot cũ.

Đăng xuất phải hủy phiên server và loại cache dữ liệu cá nhân phía client.
Preview production phải dùng fixture công khai riêng, tuyệt đối không dùng
`get(null)` để lấy dữ liệu tài khoản thật đầu tiên như mock hiện tại.

### B6. Bước 6 — Thay `save(callback)` bằng lệnh JSON

1. Khai báo DTO tạo/sửa cho từng tài nguyên và error type; tách body request khỏi
   snapshot response, không dùng một kiểu `Partial<Garage>` để cho phép sửa mọi thứ.
2. Tạo adapter HTTP: base URL cấu hình, JSON, cookie nếu chọn, xử lý status/ETag,
   hủy request và chuẩn hóa lỗi. Chỉ cấu hình biến môi trường khi thực sự tích hợp;
   mọi biến `VITE_*` đều công khai, không đặt secret ở đó.
3. Thay các callback trong `Dialogs.tsx`, `pages.tsx`, `store.tsx` bằng mutation
   có tên như `createEntry(input)`, `updateEntry(id, input)`, `deletePlan(id)`.
   Không stringify/eval/gửi source của callback và không tải cả Garage lên để ghi đè.
4. Giữ `useGarage()` làm mặt tiền nếu hữu ích để tránh thay đổi nhiều màn hình;
   có thể giữ adapter mock trong giai đoạn chuyển đổi, nhưng phải đi cùng contract.
5. Cache vẫn chia theo tài khoản; adapter lưu snapshot/ETag đồng bộ.
   Chọn một chiến lược cập nhật snapshot hoặc invalidation/refetch nhất quán.
   Xử lý 401 bằng xóa phiên/cache; 412 bằng refetch và yêu cầu người dùng kiểm tra lại.
6. Giữ thông báo pending/thành công/lỗi và hành vi chỉ đóng form khi thành công.
7. Archive cần thêm field có kiểu rõ ràng, bộ lọc/chọn xe hoạt động, trạng thái
   chỉ đọc ở tất cả màn hình; chỉ ẩn nút là chưa đủ, server phải chặn ghi.
8. Đổi nội dung “chỉ lưu bộ nhớ” khi backend thật đã hoạt động, không đổi trước.
9. Không duy trì hai nguồn tính nghiệp vụ mâu thuẫn; kiểm thử đối chiếu client/server.

### B7. Bước 7 — Migration, kiểm thử và phát hành

- Tạo migration có version và kế hoạch rollback/backup phù hợp công nghệ được chọn.
- Tạo settings mặc định khi tạo user trong cùng transaction.
- Seed demo chạy có chủ đích, idempotent, tách môi trường; không tự seed lại khi
  server khởi động và không đưa mật khẩu mẫu vào tài khoản production.
- Nếu cần nhập mock, ánh xạ `(accountId, oldId)` sang ID mới cho từng loại tài
  nguyên, cập nhật đủ FK; không nhập dữ liệu người dùng nếu chưa có yêu cầu.
- Chuyển các ca trong `tests/data.test.ts` thành kiểm thử domain/API tương đương.
- Bổ sung integration test cho ownership/IDOR, archive, rollback, request đồng
  thời, idempotency, revision cũ, xóa plan giữ chi phí, xóa service tính lại hạn,
  login/logout và dữ liệu còn sau restart.
- Kiểm thử ngày cuối tháng/năm nhuận, biên 0 km/0 ngày, nhiều ODO cùng ngày,
  sửa lịch sử giữa hai mốc và sửa ODO ban đầu.
- Đối chiếu báo cáo/CSV với cùng tập dữ liệu, cùng owner, cùng bộ lọc; nếu phân
  trang server thì tổng/CSV không được chỉ lấy trang đang hiển thị.
- Chạy kiểm tra frontend chuẩn và lệnh backend được bổ sung vào tài liệu sau
  khi chốt stack; không tự bịa lệnh backend ở trạng thái hiện tại.
- Chỉ báo backend sẵn sàng khi đã kiểm chứng persistence, phân quyền và các
  bất biến; nếu chưa đủ, ghi rõ phần nào mới là thiết kế hoặc mock.
