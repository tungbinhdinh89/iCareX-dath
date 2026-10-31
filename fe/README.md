# Frontend

Frontend React + Vite của VehicleCare. Mã nguồn được tách riêng để frontend có
thể được build và deploy độc lập với ASP.NET Core API.

Các lớp Clean Architecture trong `src/`:

- `app/`: composition root, routing và cấu hình ứng dụng.
- `features/`: use case và UI theo từng nghiệp vụ.
- `domain/`: entity, value object và quy tắc nghiệp vụ thuần.
- `infrastructure/`: HTTP client, storage và adapter bên ngoài.
- `shared/`: component, hook và tiện ích dùng chung.

Auth đã dùng API thật qua `/api/v1/auth` (CSRF, register, login, me, logout);
garage vẫn dùng mock data cho tới khi business API backend hoàn tất.
