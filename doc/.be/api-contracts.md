# API contracts — thiết kế cho giai đoạn tiếp theo

Health/Scalar/OpenAPI và nhóm `/api/v1/auth` đã có HTTP controller. Các route
garage/business còn lại dưới đây CHƯA triển khai.

## Quy ước

- JSON camelCase; Guid là chuỗi opaque, ngày YYYY-MM-DD; timestamp ISO UTC.
- Snapshot `Garage`: vehicles[], entries[], plans[], profile{name,email,phone}, settings{reminders,thresholdKm,thresholdDays}.
- Field như frontend; vehicle thêm archivedAt nullable. Không expose EF entity hoặc Identity model trực tiếp.
- Kind fuel/service/odo; vehicle type Ô tô/Xe máy. planId nullable; image ánh xạ ImageUrl.
- GET Garage trả snapshot và ETag dạng `"<revision>"`, read snapshot/revision trong cùng repeatable-read transaction.
- Mutation nhận DTO field allowlist, If-Match bắt buộc; thiếu=428, không khớp=412. Tạo trả 201, sửa/xóa/archive trả 200 kèm Garage/ETag.
- Ngoại lệ auth không dùng Garage revision; logout 204. Đăng ký 201 Account, không tự login; tạo settings atomically.
- POST entries yêu cầu Idempotency-Key dài 1..128, scope theo owner+key. Hash method/path/canonical payload, replay cùng payload trả kết quả cũ; payload khác=409.
- Kiểm tra idempotency replay trước If-Match để retry thành công không bị stale revision; client refetch sau replay tránh ghi snapshot cũ vào cache mới. Key lưu 24h, hết hạn được thay thế trong transaction. Chưa có cleanup job.
- PATCH chỉ nhận trường có thể sửa; phân biệt omitted/null, reject field lạ, không nhận userId/owner, computed fields hoặc toàn bộ Garage.

## Routes

| Method/path `/api/v1` | Input | Output |
|---|---|---|
| GET /auth/csrf | Không | Token, anti-forgery cookie |
| POST /auth/register | name,email,password,phone? + CSRF | Account, 201; không tự login; settings true/500/30 |
| POST /auth/login | email,password + CSRF | Account + cookie, 200 |
| GET /auth/me | Cookie | Account + roles hoặc 401 |
| POST /auth/logout | Cookie + CSRF | Thu hồi session, clear cookie, 204 |
| GET /garage | Cookie | Garage + ETag |
| POST /vehicles | name,plate,type,year,baseOdo,image | Garage, 201 |
| PATCH /vehicles/{id} | name,plate,type,year,baseOdo,image | Garage |
| DELETE /vehicles/{id} | Không | Garage; cascade active vehicle |
| POST /vehicles/{id}/archive | Không | Garage; chưa có restore |
| POST /entries | vehicleId,kind,title,date,odo,cost,liters,note,planId? | Garage; Idempotency-Key |
| PATCH /entries/{id} | title,date,odo,cost,liters,note | Garage; không đổi vehicle/kind/planId |
| DELETE /entries/{id} | Không | Garage; tính lại dữ liệu suy ra |
| POST /plans | vehicleId,title,dueDate,dueOdo,intervalKm,intervalMonths | Garage |
| PATCH /plans/{id} | title,dueDate,dueOdo,intervalKm,intervalMonths | Garage; không đổi xe |
| DELETE /plans/{id} | Không | Garage; giữ service, bỏ planId |
| PATCH /profile | name,phone | Garage; email chưa cho sửa |
| PATCH /settings | reminders,thresholdKm,thresholdDays | Garage |

Không cần endpoint complete riêng: POST entry service có planId là hoàn tất.
Query báo cáo/CSV giai đoạn đầu có thể tính từ snapshot bằng helper FE hiện tại; khi thêm query server phải dùng cùng tập lọc. Chưa bổ sung route report/export độc lập.

## Auth và lỗi

Identity email/user name chuẩn hóa unique; password policy dự kiến tối thiểu 12 ký tự, có chữ hoa/thường/số/ký tự đặc biệt.
Cookie HttpOnly, Secure ngoài localhost Development, SameSite=Lax khi cùng site.
Session lifetime 8h không sliding; protected ticket trong AuthSession. Mọi request kiểm tra session chưa revoke/hết hạn.
Persist Data Protection keys ngoài source; cùng key ring khi scale instance.
CSRF cho mọi POST/PATCH/DELETE kể cả login/register/logout; CORS allowlist origin với credentials, không wildcard.
Topology mặc định cùng site qua reverse proxy `/api`; FE dev dùng proxy, không tự SameSite=None cho mọi môi trường.
Login lockout 5 lần/15 phút đã cấu hình qua Identity; rate limit auth và persistent Data Protection key ring còn phụ thuộc môi trường triển khai.

```json
{"error":{"code":"ODO_OUT_OF_SEQUENCE","message":"ODO không nhất quán với lịch sử.","fields":{"odo":"ODO ngoài khoảng hợp lệ."}}}
```

401 chưa login; 403 archive; 404 không tồn tại/khác owner; 409 plate/idempotency conflict;
412 revision cũ; 422 validation nghiệp vụ; 428 thiếu If-Match; 400 malformed JSON/thiếu header bắt buộc khác;
500 nội bộ không lộ SQL/secret. Api nền tảng hiện dùng ProblemDetails cho lỗi host, chưa phải envelope nghiệp vụ.
## Vehicle and ODO API (T03)

Authenticated endpoints use the current session account; `accountId` is never
accepted from the client. Vehicle mutations use the `version` field and return
`409 Conflict` when the version is stale.

- `GET /api/v1/vehicles?includeArchived=false`
- `GET /api/v1/vehicles/{vehicleId}`
- `POST /api/v1/vehicles`
- `PUT /api/v1/vehicles/{vehicleId}`
- `POST /api/v1/vehicles/{vehicleId}/archive`
- `POST /api/v1/vehicles/{vehicleId}/restore`
- `GET /api/v1/vehicles/{vehicleId}/odometer-readings`
- `POST /api/v1/vehicles/{vehicleId}/odometer-readings`
- `PUT /api/v1/vehicles/{vehicleId}/odometer-readings/{readingId}`
- `DELETE /api/v1/vehicles/{vehicleId}/odometer-readings/{readingId}?version={version}`

Archive preserves vehicle/history data and blocks mutations. ODO dates cannot
be in the future; ODO values must remain between the previous and next reading
for the same vehicle. Fuel/service APIs remain scoped to later tickets.
