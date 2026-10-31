# Backend changelog

## 2026-10-31 — Auth/session API

- Đăng ký tài khoản atomically với Identity user, Account và UserSettings mặc định.
- Thêm login/logout/me và CSRF token endpoint dưới `/api/v1/auth`.
- Cookie HttpOnly, SameSite=Lax, session server-side 8 giờ; mỗi request kiểm tra revoke/expiry.
- Cấu hình Identity password policy và lockout 5 lần trong 15 phút; không tự login sau đăng ký.
- Thêm host coverage cho CSRF và unauthenticated `/me`. Các mutation/business API và rate limit vẫn chưa triển khai.
- Refactor auth theo controller + repository/service boundary: shared DTO contract,
  Application interface, Infrastructure implementation, Identity role `User` và
  DI extension. Frontend gọi CSRF/register/login/me/logout thật qua Vite proxy,
  hiển thị notification cho thành công/thất bại.
- Chuyển cấu hình database cục bộ sang .NET User Secrets (`VehicleCare.Api`);
  script backend không còn đọc `.env` và `.env.example` đã được loại bỏ.
- Thêm preflight cho các action cần database để báo chính xác khi thiếu
  `ConnectionStrings:VehicleCare`, thay vì lỗi EF/connection chung chung.
- Sửa `be.ps1 run` mặc định bind `http://localhost:5080`, tránh đụng port 5000
  đang dùng và khớp với Vite proxy/Scalar documentation.
- Thêm kiểm tra placeholder trong User Secrets và trả HTTP 503 rõ ràng khi auth
  không kết nối được database, thay vì trả HTTP 500 chung chung.
- Sửa kiểm tra User Secrets không nhầm giá trị mật khẩu đã được CLI che thành
  placeholder; API đang chạy trên 5080 được giữ nguyên, không chạy instance thứ hai.
- Tường minh hóa JSON body binding cho register/login và giữ `Content-Type:
  application/json` ổn định trong frontend auth client.
- Tích hợp Serilog console/file rolling cho API và DatabaseTool; thêm request
  logging, auth logging có structured properties và hướng dẫn tránh ghi secret.
- T03: thêm Vehicle CRUD/list/detail, archive/restore, optimistic version
  conflict và ODO manual CRUD theo owner; tạo migration cho version và nối FE
  Vehicle/ODO vào API, loại bỏ garage seed/mock data.

## 2026-10-30 — Nền tảng đầu tiên

- Thêm solution .NET 10 gồm Domain/Application/Infrastructure/Api, UnitTests/IntegrationTests và DatabaseTool.
- Thêm PostgreSQL mappings, Identity/session/idempotency model, domain ODO/plan rules, migration và health/Scalar.
- Thêm bộ FR/ERD/contract/flow/development/testing; nối hướng dẫn agent từ root và be.
- Chưa triển khai auth/business APIs hoặc nối frontend. Kết quả chạy thực tế tại testing.md.
- Đã tạo/migrate vehiclecare trên PostgreSQL 18.6 và pass persistence smoke (cleanup fixture). 12 unit + 2 host tests pass; database Testcontainers bị chặn do không có Docker daemon.
- Nâng dependency có NuGet advisory lên bản vá; dùng console logging và deferred DI configuration/validate-on-start để HTTP host test không truy cập nhầm cấu hình database thật.
