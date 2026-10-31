# Hướng dẫn backend VehicleCare

ASP.NET Core 10 monolith, Clean Architecture, PostgreSQL và Scalar.
Agent chuyên backend phải đọc `be/AGENTS.md` trước khi sửa.

## Thứ tự đọc

1. [Kiến trúc/package](architecture.md).
2. [Yêu cầu/trạng thái](requirements.md).
3. [Entity/ERD](data-model.md).
4. [API dự kiến](api-contracts.md).
5. [Flow tính năng](functional-flows.md).
6. [Phát triển](development-guide.md), [kiểm thử](testing.md), [changelog](changelog.md).

## Hiện trạng

Đã có project, model/mapping, domain rules ODO/kế hoạch, migration, health endpoints và công cụ database.
Kết quả chạy thực tế tại [testing.md](testing.md).
Auth/session API đã triển khai; API nghiệp vụ garage vẫn chưa có. Frontend auth
đã gọi API thật, còn dữ liệu garage vẫn mock và không được mô tả đã lưu PostgreSQL.

## Cấu hình và chạy

Cần SDK .NET 10; `be/global.json` cho phép feature version ổn định mới hơn trong major 10.
Script ưu tiên SDK riêng tại `be/.tools/dotnet`, nếu không có dùng PATH.
Backend dùng .NET User Secrets với `UserSecretsId=VehicleCare.Api`; secret được
lưu ngoài repo. Khởi tạo bằng:

```powershell
dotnet user-secrets --project be/src/VehicleCare.Api set "ConnectionStrings:VehicleCare" "<local-connection-string>"
dotnet user-secrets --project be/src/VehicleCare.Api set "ASPNETCORE_ENVIRONMENT" "Development"
```

Process environment có thể override User Secrets cho CI; script không đọc `.env`
và không nạp connection string vào file trong repository. Các action cần database
(`run`, `check`, `provision`, `smoke`, `migration`, `migrate`) sẽ báo rõ nếu thiếu
key `ConnectionStrings:VehicleCare`.

```powershell
# Chạy từ root repo; ExecutionPolicy chỉ áp dụng process này.
powershell -NoProfile -ExecutionPolicy Bypass -File be/scripts/be.ps1 restore
powershell -NoProfile -ExecutionPolicy Bypass -File be/scripts/be.ps1 build
powershell -NoProfile -ExecutionPolicy Bypass -File be/scripts/be.ps1 test
powershell -NoProfile -ExecutionPolicy Bypass -File be/scripts/be.ps1 check
# Provision chỉ tạo database chưa có, cần CREATEDB; không sửa database đã tồn tại.
powershell -NoProfile -ExecutionPolicy Bypass -File be/scripts/be.ps1 provision
powershell -NoProfile -ExecutionPolicy Bypass -File be/scripts/be.ps1 migrate
powershell -NoProfile -ExecutionPolicy Bypass -File be/scripts/be.ps1 smoke
powershell -NoProfile -ExecutionPolicy Bypass -File be/scripts/be.ps1 run
```

Đích LAN: `192.168.1.200:5433`, database `vehiclecare`.
`ConnectionStrings:VehicleCare` được đọc từ User Secrets ở runtime và EF
design-time; `ConnectionStrings__VehicleCare` chỉ dành cho override của môi trường
triển khai/CI.
`be.ps1 run` mặc định bind `http://localhost:5080` để FE proxy `/api` gọi đúng
API; có thể override bằng `ASPNETCORE_URLS`.
Không dùng placeholder `Host=...`, `Username=...` hoặc `Password=...` trong
User Secrets; script sẽ chặn các giá trị này trước khi chạy action cần database.
Không commit secret, không dùng `VITE_*`, không bật sensitive data logging.
`dotnet user-secrets list` che mật khẩu thành `******`; đây là hành vi bình
thường và không phải placeholder. Placeholder cần tránh là `Host=...`,
`Username=...` hoặc `Database=...`.
Không tự migrate/seed lúc startup. Không đổi Docker container/volume NAS.
`check` in database/version/TLS thực tế, không in credential.
Không ép SSL Disable hoặc bỏ kiểm tra certificate; production cần TLS theo hạ tầng.

`/health/live` kiểm tra process; `/health/ready` kiểm tra kết nối (không xác nhận schema).
Scalar `/scalar/v1`, OpenAPI `/openapi/v1.json` chỉ có trong Development.

### Logging

API dùng `Serilog.AspNetCore` với `Serilog.Sinks.Console` và
`Serilog.Sinks.File`. Log được ghi ra console khi chạy `run`/`dotnet watch` và
file rolling theo ngày tại `be/src/VehicleCare.Api/logs/vehiclecare-YYYYMMDD.log`;
DatabaseTool ghi cùng loại log cho `check`, `provision` và `smoke`.
Request log gồm method, path, status code và elapsed time. Auth log chỉ ghi
trạng thái, account id và email đã che; tuyệt đối không ghi password, token,
cookie, request body nhạy cảm hoặc connection string. Không commit thư mục
`logs/`.

Auth controller ở `src/VehicleCare.Api/Controllers/AuthController.cs`; contract
và interface ở `src/VehicleCare.Application/Services/Contracts`, implementation
ở `src/VehicleCare.Infrastructure/Services/Implementations`. Entity database
chỉ nằm trong Domain/Infrastructure; không trả EF entity trực tiếp ra API.
Role `User` được tạo khi đăng ký và được đưa vào cookie claim. Chưa seed Admin,
vì vậy phân quyền role mới là nền tảng cho các business controller tiếp theo.

## Cập nhật tài liệu

Mỗi thay đổi backend phải cập nhật trang liên quan và changelog cùng thay đổi.
Không gắn nhãn đã triển khai cho thiết kế chưa có code; không ghi test pass khi chưa chạy.
