# Quy trình agent backend

1. Đọc AGENTS gốc, `be/AGENTS.md`, README bộ này và trang liên quan; kiểm tra git status.
2. Đối chiếu FR/acceptance với rules.md và frontend; không coi mock client là bảo mật server.
3. Thêm use case theo feature vào Application; khai báo DTO/port tối thiểu, domain rules thuần C#.
4. Implement adapter Infrastructure và HTTP Api; luôn xét owner/archive/revision/idempotency/transaction.
5. Schema change: tạo migration, review SQL và kiểm thử PostgreSQL riêng. Không tự áp schema lúc startup.
6. Chạy kiểm thử tương ứng, ghi exit code/kết quả thực tế; không sửa test để che regression.
7. Cập nhật requirements/data/API/flow/architecture/testing nếu liên quan; luôn thêm changelog.
8. Review diff: không secret, không frontend thay ngoài scope, không artifact .tools/bin/obj; không tự commit/push/deploy.

## Cấu hình

Connection string dùng .NET User Secrets với `UserSecretsId=VehicleCare.Api`;
không lưu credential trong `.env` hoặc source control.
Script không parse file cấu hình bí mật; `ConnectionStrings__VehicleCare` chỉ là
override dành cho môi trường triển khai/CI.
Không dùng parser env này cho shell code. Env từ process có ưu tiên cao hơn file.
Thiếu connection string fail sớm; không fallback password trong source/appsettings.
SDK install ở `.tools` bị ignore; máy khác có thể dùng SDK hệ thống major 10.

## Database operations

- `check`: read-only xác thực và đọc version/TLS.
- `provision`: chỉ CREATE DATABASE vehiclecare nếu chưa tồn tại, dùng database postgres để kiểm tra catalog; cần quyền.
- `migrate`: chạy EF migration rõ ràng; xem schema hiện tại/backup nếu đã có dữ liệu.
- `smoke`: tạo fixture GUID riêng, commit, đọc bằng DbContext khác, cleanup đúng user fixture bằng cascade; không reset database.
- Nếu connection fail: kiểm tra route/firewall TCP 5433, Docker port mapping, pg_hba.conf, auth và quyền. Không tự thay NAS settings.
- Nếu CREATEDB bị từ chối: người quản trị chạy `CREATE DATABASE vehiclecare OWNER tungbinhdinh89;`, rồi chạy check/migrate.

## Logging

- Dùng `ILogger<T>` trong service/controller; message dùng structured logging
  với property đặt tên, không nối chuỗi thủ công.
- API request log được Serilog ghi tự động ra console và file rolling theo ngày.
- Auth log trạng thái register/login, account id và email đã che; không ghi
  password, token, cookie, connection string hoặc payload nhạy cảm.
- File log nằm trong `be/src/VehicleCare.Api/logs/`; không commit hoặc đưa log
  chứa dữ liệu nhạy cảm lên issue/chat.
- Khi kiểm tra migration/database tool, xem console hoặc file log do
  `VehicleCare.DatabaseTool` tạo; thông tin credential luôn bị loại khỏi message.

## Tài liệu cũ

sumary/skill/rules ở gốc vẫn mô tả frontend prototype và yêu cầu. Chỉ dẫn mới về backend nằm bộ này.
Auth frontend đã nối thật; chỉ thay mock garage khi business API hoàn tất và có
DTO/ownership/revision tương ứng.
