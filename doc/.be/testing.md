# Kiểm thử và kết quả

## Lệnh

Chạy từ root: `powershell -NoProfile -ExecutionPolicy Bypass -File be/scripts/be.ps1 <action>`.

| Action | Ý nghĩa |
|---|---|
| build | Build toàn solution |
| test | UnitTests và HTTP Host tests, không cần Docker hoặc DB thật |
| integration | PostgreSQL Testcontainers, cần Docker đang chạy |
| check | Npgsql connect đích và đọc database/version/TLS |
| migrate | Áp migration rõ ràng |
| smoke | Commit/read/cleanup fixture trên đích |

## Coverage hiện có

- ODO earlier/later/same-day, loại chính entry đang sửa, xóa max giảm current ODO, base ODO.
- Fuel/service cost/liters, archive guard, numeric precision.
- Latest service date/odo, sửa/xóa service, tháng cuối/năm nhuận, hạn OR và đúng hạn soon, không cộng thêm cost.
- Reflection architecture guard Domain/Application không phụ thuộc outer layer.
- Integration: migrate PostgreSQL riêng, persistence, duplicate plate, ownership FK, kind constraint, unlink plan giữ chi phí, cascade vehicle; health/OpenAPI/Scalar qua WebApplicationFactory.
- Host tests độc lập Docker: liveness/OpenAPI/Scalar, readiness 503 khi DB unavailable và docs 404 trong Production.
- Host auth test: cấp CSRF token, từ chối mutation thiếu CSRF và trả 401 cho `/auth/me` khi chưa xác thực.
- Kết quả mới nhất: `scripts/be.ps1 test` pass 12 unit tests và 3 host tests.

Testcontainers không lấy connection từ User Secrets/NAS và không dùng NAS. Test
thiếu Docker phải báo thất bại, không silently skip/pass.
Khi thêm API phải bổ sung IDOR, CSRF/session revoke, archive mọi đường ghi, rollback/concurrency/If-Match/idempotency và report/CSV parity.

## Kết quả phiên triển khai 2026-10-30

- SDK riêng .NET 10.0.403 đã cài ở `.tools`; restore thành công với EF/Microsoft 10.0.14, Npgsql provider 10.0.3, Scalar 2.18.6, Testcontainers 4.16.0. Các dependency bị NuGet advisory ở lần restore đầu đã được nâng bản vá; không tắt audit.
- Build solution thành công, 0 warning/0 error.
- UnitTests: 12 pass. Host tests không Docker: 2 pass. `scripts/be.ps1 test` exit 0.
- Integration PostgreSQL Testcontainers: 2 test database không chạy được do Docker daemon unavailable (không có docker CLI/pipe docker_engine); lần chạy trả failed, không đánh dấu pass/skip. Cấu hình image PostgreSQL 18 tương ứng major server đích; chạy lại `integration` khi có Docker.
- Đã tạo database `vehiclecare` trước đó chưa tồn tại trên `192.168.1.200:5433`, áp migration `20261030034612_InitialCreate` thành công.
- `check` exit 0: PostgreSQL 18.6, TLS false (trạng thái thực tế LAN; không ép SSL Disable trong code).
- `smoke` exit 0: ghi commit fixture GUID riêng, đọc qua DbContext khác, xóa đúng fixture. Không seed demo, không thay container/volume.
- API thực chạy tạm: `/health/live`, `/health/ready`, `/openapi/v1.json`, `/scalar/v1` đều HTTP 200 khi process có quyền mạng tới NAS; đã dừng process kiểm chứng.
- Khi chạy trong sandbox hạn chế mạng, ready trả 503 như thiết kế; kiểm tra ngoài sandbox đã xác nhận 200.

Giới hạn: chưa kiểm chứng database constraints/delete policies và register/login
với PostgreSQL Testcontainers do thiếu Docker; database smoke chỉ xác nhận
persistence. Cần chạy lại integration auth với Docker trước nghiệm thu.
