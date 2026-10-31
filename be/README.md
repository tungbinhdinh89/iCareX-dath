# Backend

Backend dùng ASP.NET Core 10 Web API và được triển khai độc lập với frontend,
theo ranh giới Clean Architecture:

```text
src/
  VehicleCare.Api/             # HTTP endpoints, middleware, composition root
  VehicleCare.Application/     # use cases, ports, DTOs
  VehicleCare.Domain/          # entities, value objects, domain rules
  VehicleCare.Infrastructure/  # EF Core, PostgreSQL, external adapters
tests/
  VehicleCare.UnitTests/
  VehicleCare.IntegrationTests/
```

Nền tảng backend đã được tạo. Bắt đầu tại [hướng dẫn backend](../doc/.be/README.md)
và [hướng dẫn agent](AGENTS.md). Auth API đã có tại `/api/v1/auth`; các API
nghiệp vụ vehicle/entry/plan chưa triển khai và frontend vẫn là mock.

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File be/scripts/be.ps1 restore
powershell -NoProfile -ExecutionPolicy Bypass -File be/scripts/be.ps1 build
powershell -NoProfile -ExecutionPolicy Bypass -File be/scripts/be.ps1 test
powershell -NoProfile -ExecutionPolicy Bypass -File be/scripts/be.ps1 check
powershell -NoProfile -ExecutionPolicy Bypass -File be/scripts/be.ps1 migrate
powershell -NoProfile -ExecutionPolicy Bypass -File be/scripts/be.ps1 run
```

SDK .NET 10; dùng .NET User Secrets để lưu credential cục bộ bên ngoài repo.
Scalar: `http://localhost:5080/scalar/v1`; health: `/health/live`, `/health/ready`.

## Logging

Backend dùng `Serilog.AspNetCore`, `Serilog.Sinks.Console` và
`Serilog.Sinks.File`. Khi chạy `be.ps1 run`, `dotnet run` hoặc `dotnet watch`,
log có cấu trúc được ghi đồng thời ra console và file rolling theo ngày tại
`be/src/VehicleCare.Api/logs/vehiclecare-YYYYMMDD.log`. DatabaseTool cũng ghi
log khi chạy `check`, `provision` hoặc `smoke`.

Request log có method, path, status code và thời gian xử lý. Auth log ghi trạng
thái đăng ký/đăng nhập, account id và email đã che. Không ghi password, token,
cookie, connection string hoặc request body nhạy cảm. Thư mục `logs/` không được
commit; khi điều tra lỗi có thể xem file log ngày hiện tại hoặc console output.
