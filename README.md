# VehicleCare

Backend foundation đã có tại `be/`: ASP.NET Core 10, Clean Architecture, EF Core/PostgreSQL và Scalar.
Xem [hướng dẫn backend](doc/.be/README.md) và [kết quả kiểm chứng](doc/.be/testing.md).
Auth API và tích hợp frontend cho đăng ký/đăng nhập/me/logout đã triển khai.
Các màn hình garage vẫn dùng mock data cho tới khi các business API được triển khai;
không coi dữ liệu garage trên FE là dữ liệu đã lưu PostgreSQL.

## Kiến trúc thư mục

Repository được tổ chức theo ranh giới triển khai và hướng tới Clean
Architecture:

```text
fe/                  # React + Vite frontend
  src/
  public/
  tests/
be/                  # ASP.NET Core 10 API foundation, entities và migrations
  src/
  tests/
infra/               # Docker, PostgreSQL và cấu hình triển khai NAS (sẽ triển khai sau)
.github/workflows/   # GitHub Actions (sẽ triển khai sau)
```

`fe/` chứa frontend prototype; `be/` chứa nền tảng API chạy được cùng entity/migration.
PostgreSQL chạy trong Docker trên NAS; database `vehiclecare` được migrate bằng lệnh
chủ động, không tự migrate khi API khởi động.
`infra/` dành cho cấu hình triển khai sau này; chưa tạo workflow CI/CD.

Frontend quản lý xe cá nhân bằng tiếng Việt, xây dựng với React 19,
TypeScript, Vite, Material UI Community + Icons, token Astra, React Router Data
Mode, TanStack Query, React Hook Form + Zod và Chart.js.

## Chạy dự án

### Frontend

Yêu cầu Node.js và pnpm:

```powershell
pnpm install
pnpm dev
```

Vite proxy `/api` tới `http://localhost:5080`; đặt `VEHICLECARE_API_URL` nếu API
chạy ở địa chỉ khác. Chạy backend trước khi kiểm tra đăng ký/đăng nhập.

Kiểm tra frontend:

```powershell
pnpm exec tsc --noEmit
pnpm test
pnpm build
```

### Backend

Yêu cầu .NET SDK 10.0 và PostgreSQL. Backend dùng .NET User Secrets; connection
string không nằm trong source hoặc file `.env`.

Khởi tạo secret từ root repo (lệnh này lưu ngoài repository):

```powershell
dotnet user-secrets --project be/src/VehicleCare.Api set "ConnectionStrings:VehicleCare" "Host=...;Port=5433;Database=vehiclecare;Username=...;Password=..."
dotnet user-secrets --project be/src/VehicleCare.Api set "ASPNETCORE_ENVIRONMENT" "Development"
```

Không dùng giá trị mẫu như `Host=...`, `Username=...` hoặc `Password=...`.
`dotnet user-secrets list` hiển thị mật khẩu thành `Database=...`.`; đây là giá trị đã
được che và không phải lỗi cấu hình. Giá trị placeholder mới là nguyên nhân
khiến đăng ký trả lỗi database unavailable.

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File be/scripts/be.ps1 restore
powershell -NoProfile -ExecutionPolicy Bypass -File be/scripts/be.ps1 build
powershell -NoProfile -ExecutionPolicy Bypass -File be/scripts/be.ps1 test
powershell -NoProfile -ExecutionPolicy Bypass -File be/scripts/be.ps1 run
```

Provision database (chỉ khi database chưa tồn tại), tạo migration mới và cập nhật
schema hiện tại:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File be/scripts/be.ps1 provision
powershell -NoProfile -ExecutionPolicy Bypass -File be/scripts/be.ps1 migration -Name AddFeatureName
powershell -NoProfile -ExecutionPolicy Bypass -File be/scripts/be.ps1 migrate
```

`migrate` chạy `dotnet ef database update` với connection string từ User Secrets
(process environment vẫn có thể override trong CI); không reset database và không
chạy tự động lúc startup.
Auth API ở `/api/v1/auth`; Scalar ở `http://localhost:5080/scalar/v1` trong
Development. Lệnh `be.ps1 run` mặc định dùng `http://localhost:5080` để khớp
Vite proxy; có thể override bằng biến `ASPNETCORE_URLS`. FE dùng cookie HttpOnly,
session server-side và CSRF token qua
proxy `/api`. Đăng ký tạo Identity user, `Account` và `UserSettings` trong cùng
transaction; kiểm tra dữ liệu bằng PostgreSQL sau khi migrate:

```powershell
dotnet user-secrets --project be/src/VehicleCare.Api list
```

Sau khi đăng ký, kiểm tra bảng `accounts` bằng công cụ PostgreSQL của bạn với
connection string được đọc trực tiếp từ User Secrets; không copy connection
string vào source, `.env`, terminal history hoặc frontend.

## Trải nghiệm

Trang tổng quan hiển thị dữ liệu minh họa. Chọn **Đăng nhập** hoặc **Tạo tài
khoản**; tài khoản mới được lưu vào PostgreSQL và không tự đăng nhập sau đăng ký.

- Đăng ký tài khoản lưu Identity user, Account và UserSettings mặc định atomically
  trong PostgreSQL; mật khẩu chỉ lưu dưới dạng hash.
- Đăng nhập dùng cookie HttpOnly và session server-side; đăng xuất thu hồi session.
- Lỗi validation, đăng ký thất bại, đăng nhập thất bại và đăng nhập thành công
  đều hiển thị Alert/notification trên FE.
- Role mặc định `User` được lưu bằng ASP.NET Identity và role claim được đưa vào
  cookie; endpoint nghiệp vụ có thể dùng `[Authorize(Roles = "...")]`. Chưa có
  tài khoản quản trị hoặc business endpoint cần role trong giai đoạn này.
- Đăng xuất không xóa dữ liệu trong bộ nhớ của tab; đăng nhập lại cùng tài khoản
  trong tab đó vẫn thấy thay đổi cho đến khi tải lại trang.
- Nhắc bảo dưỡng chỉ hiển thị trong ứng dụng khi web đang mở.
- Định dạng VNĐ, km, lít, dd/MM/yyyy; múi giờ Asia/Ho_Chi_Minh.

## Nghiệp vụ

- Quản lý nhiều ô tô/xe máy, ODO ban đầu và lịch sử ODO thủ công.
- Nhập, sửa, xóa nhiên liệu, dịch vụ và ODO; kiểm tra ODO với ngày trước/sau.
  Trong một ngày có thể có nhiều bản ghi với các ODO khác nhau.
- ODO hiện tại, báo cáo và chi phí được tính từ cùng một tập lịch sử.
- Kế hoạch đến hạn theo ngày hoặc km, tùy điều kiện nào đến trước.
- Hoàn tất kế hoạch bằng một dịch vụ có chi phí. Dịch vụ được liên kết với kế
  hoạch; không tạo thêm một khoản chi phí riêng.
- Kỳ kế tiếp được tính từ dịch vụ liên kết gần nhất. Sửa/xóa dịch vụ tự động
  tính lại kế hoạch; lịch cuối tháng được chặn về ngày hợp lệ cuối cùng.
- Lọc lịch sử, xem biểu đồ, xuất CSV theo bộ lọc, sửa hồ sơ và cấu hình nhắc nhở.

## Cấu trúc chính

- `shared/auth-contracts.ts`: DTO auth dùng chung cho FE; contract C# tương ứng
  nằm ở `be/src/VehicleCare.Application/Services/Contracts`.
- `fe/src/data.ts`: API auth thật, mock garage API và quy tắc nghiệp vụ hiện tại.
- `fe/src/store.tsx`: phiên trong bộ nhớ, TanStack Query và thao tác dùng chung.
- `fe/src/app/routes.tsx`: cấu hình React Router.
- `fe/src/Layout.tsx`: điều hướng desktop/mobile và khung ứng dụng.
- `fe/src/pages.tsx`: các màn hình.
- `fe/src/Dialogs.tsx`: biểu mẫu có validation và xác nhận xóa.
- `fe/src/components.tsx`: biểu đồ, bảng lịch sử và component dùng chung.
- `fe/src/index.css`: phông chữ tự lưu trữ, token Astra tùy chỉnh navy/xanh dương.
- `fe/tests/data.test.ts`: kiểm thử các quy tắc nghiệp vụ.

## Kiểm tra

```sh
pnpm format fe/src fe/tests
pnpm exec tsc --noEmit
pnpm test
pnpm build
```

Server phát triển trong Figma Make đã được môi trường chạy sẵn. Không cần khởi
động thêm server. Khi chạy cục bộ, dùng `pnpm dev` và backend `be.ps1 run`.
Nguồn và giấy phép tài nguyên tại `fe/public/ASSET-CREDITS.md`.
