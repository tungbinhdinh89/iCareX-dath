# iCareX

Web quản lý phương tiện cá nhân, ODO, chi phí và kế hoạch bảo dưỡng. Backend ASP.NET Core 10; frontend React 19/TypeScript/Vite; PostgreSQL qua EF Core.

Authentication/Vehicle authorization hiện có: [runtime contract và cấu hình SMTP](doc/.be/access-control.md).
Account mới phải xác thực email trước khi vào ứng dụng; setup `AccountEmail:*`
bằng User Secrets/environment để gửi thư thật. Có permissions API, membership,
invitation, verify/reset/change password; chưa có Fuel/Maintenance/Reminder CRUD
hoặc ownership transfer. Các migration sharing hiện có phải được áp dụng chủ
động; ứng dụng không tự migrate lúc startup.

## Cấu trúc

- `src/`: Domain, Application, Infrastructure, Shared, Web và hai project test.
- `tools/`: DatabaseTool, generator hợp đồng và SDK cục bộ trong `.local/`.
- `scripts/`: migration, kiểm tra database và chạy frontend/backend.
- `doc/`: đặc tả, kiến trúc, schema, quy tắc và hướng dẫn.
- `Vehicles_management_dath.wiki/`: baseline sản phẩm và kế hoạch.
- `stephen-skills/`: bộ skill đang có; giữ tên thư mục gốc của bộ này.
- `report_template_latex/`: template và nguồn báo cáo LaTeX.

Xem [cây repository và dependency](doc/structure-architecture.md). Các thư mục ẩn Git/GitHub, manifest tool .NET và Figma là cấu hình cần thiết.

Đặc tả auth/access mới: [authentication-authorization](doc/authentication-authorization.md),
[schema sharing](doc/schema-database.md#auth-sharing-schema) và
[test matrix](doc/.be/testing.md#access-test-matrix). Thiết kế đích bổ sung
Owner/Manager/Contributor/Viewer theo xe, invitation và transfer; source hiện
vẫn owner-only, chưa có migration/API sharing. Reuse Identity/cookie/session/
CSRF; không tạo authentication thứ hai.

## Codex agents và custom skills

Nghiệp vụ auth/access cho backend/database/frontend/test dùng
[icarex-access-control](.agents/skills/icarex-access-control/SKILL.md);
đọc phần theo vai trò, đối chiếu current/target và giữ phạm vi user giao.

Đọc [hướng dẫn dùng skills cho Codex](doc/codex-skills.md) và [quy tắc iCareX](doc/rules.md) trước khi giao việc cho agent. `AGENTS.md` ở root dẫn agent tới hai tài liệu này; chỉ đọc các skill phù hợp tác vụ, không nạp toàn bộ bộ skill mỗi phiên.

- Agent theo vai trò: `stephen-skills/.codex/agents/*.toml` — backend, frontend, PM, QA và tech lead.
- Skill chuyên môn: `stephen-skills/.agents/skills/<name>/SKILL.md`.
- Skill quy trình: `stephen-skills/skills/<name>/SKILL.md`.

Ví dụ dùng ngay bằng đường dẫn, kể cả khi chưa đăng ký skill vào Codex:

```text
Đọc AGENTS.md, doc/rules.md và developer_instructions trong
stephen-skills/.codex/agents/frontend-dev.toml.
Đọc stephen-skills/.agents/skills/frontend-design/SKILL.md trước khi sửa UI.
Chỉnh form thêm xe theo MUI/Astra hiện có, giữ UX tiếng Việt và kiểm tra phần bị ảnh hưởng.
```

Frontend đã có bản cài ở root: `.codex/agents/frontend-dev.toml` và `.agents/skills/frontend-design/SKILL.md`. Trong Codex CLI/IDE, dùng `/skills` hoặc `$frontend-design` để chọn skill; `frontend-dev` là tên agent, không phải tên skill. Nếu chưa thấy cập nhật, mở phiên Codex mới tại root iCareX. Bản cài được copy từ `stephen-skills/`, cần review và đồng bộ khi nguồn thay đổi.

Các vai trò/skill khác trong repo con `stephen-skills/` chưa tự được đăng ký từ root. Hướng dẫn chi tiết có cách cài vào `.codex/agents/` và `.agents/skills/`, bảng chọn skill và cách kiểm tra việc nạp. Không dùng các trường TOML suy đoán như `context_files` hoặc `skills = [...]`.

Một số skill dùng lại còn ví dụ CMS cũ; [rules](doc/rules.md) quy định dùng scripts, kiến trúc và design system iCareX khi áp dụng. `stephen-skills/` là repo Git riêng; `doc/` đang bị Git ignore ở repo ngoài, nên thay đổi local không tự xuất hiện trên remote.

## Chuẩn bị môi trường

Dùng PowerShell trên Windows và chạy các lệnh bên dưới từ root repository. Cần .NET SDK 10 theo `global.json`, Node.js 22, pnpm 10.34.3 theo `.mise.toml`/package manifest, cùng PostgreSQL có thể truy cập. Script backend ưu tiên `dotnet` trên PATH, fallback về `tools/.local/dotnet/dotnet.exe`. Docker chỉ cần cho nhóm integration test sử dụng Testcontainers.

```powershell
dotnet --version
node --version
pnpm --version
dotnet restore iCareX.slnx
dotnet tool restore
pnpm install
```

`pnpm install` ở root gọi postinstall để cài package frontend. Không cần chuyển node_modules về root. Nếu Windows chặn chạy file `.ps1`, dùng cú pháp đầy đủ `powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\<ten-script>.ps1 ...` cho từng lệnh, không cần đổi execution policy toàn máy.

Cấu hình database bằng .NET User Secrets; thay placeholder bằng kết nối của môi trường đang dùng, không commit giá trị thật:

```powershell
dotnet user-secrets --project src/iCareX.Web/iCareX.Web.Api set "ConnectionStrings:iCareX" "Host=<host>;Port=5432;Database=<database>;Username=<user>;Password=<password>"
```

Có thể dùng biến môi trường `ConnectionStrings__iCareX` thay User Secrets. UserSecretsId vẫn là `VehicleCare.Api` ở API/Infrastructure/DatabaseTool để cùng đọc cấu hình local hiện có; key `ConnectionStrings:VehicleCare` được hỗ trợ fallback. Key mới được ưu tiên. Script không nhận hay in connection string qua tham số. Không đặt chuỗi kết nối trong `VITE_*`. Đổi tên solution không đổi tên database hiện có.

## Dùng scripts

| Script | Cách gọi từ root | Hành vi |
| --- | --- | --- |
| [add.ps1](scripts/add.ps1) | `.\scripts\add.ps1 AddVehicleIndex` | Build API rồi tạo migration và cập nhật snapshot tại `src/iCareX.Infrastructure/Migrations`. Tên bắt đầu bằng chữ, chỉ chứa chữ/số/underscore. Không update DB. |
| [check.ps1](scripts/check.ps1) | `.\scripts\check.ps1` | Build, liệt kê migration không kết nối DB và so EF model với snapshot. Thoát lỗi nếu model cần migration. |
| check.ps1 với DB | `.\scripts\check.ps1 -Database` | Thêm việc đọc migration history từ DB đã cấu hình để chỉ ra migration chưa áp dụng; không sửa DB. Không có nghĩa là kiểm hết drift của bảng/cột thực tế. |
| [updatedb.ps1](scripts/updatedb.ps1) | `.\scripts\updatedb.ps1` | Build, áp dụng tất cả migration còn thiếu lên database được cấu hình. Có thể tạo DB nếu EF/provider và quyền hiện có cho phép. Không chạy tự động lúc start ứng dụng. |
| [checkdb.ps1](scripts/checkdb.ps1) | `.\scripts\checkdb.ps1` | Chạy DatabaseTool action check: mở kết nối, SELECT database/version/TLS. Không provision, seed hoặc update schema. |
| [run.ps1](scripts/run.ps1) | `.\scripts\run.ps1` | Build API nếu cần khởi động mới, chạy API và Vite; chờ endpoint hoạt động, gom log về terminal. |

Các script EF tự restore local tool theo `.config/dotnet-tools.json`. Restore/build cần truy cập NuGet khi cache chưa đủ. Tên `check.ps1` kiểm tra model/migration; `checkdb.ps1` chỉ xác nhận kết nối. `check.ps1 -Database` hiển thị migration `(Pending)` khi DB chưa áp dụng; chỉ lỗi so model, build hoặc kết nối mới làm lệnh thất bại.

Quy trình sau khi đổi entity/mapping:

```powershell
.\scripts\check.ps1
.\scripts\add.ps1 AddVehicleIndex
# Review migration + snapshot trước khi áp dụng lên database dự định.
.\scripts\check.ps1 -Database
.\scripts\updatedb.ps1
.\scripts\checkdb.ps1
.\scripts\check.ps1 -Database
```

`add.ps1` và `updatedb.ps1` hỗ trợ `-WhatIf` để xem hành động mà không tạo migration/update DB, và `-Confirm` nếu muốn hỏi trước khi chạy. `-WhatIf` không sinh SQL và không kiểm tra kết nối. Với database có dữ liệu, review SQL/migration và backup phù hợp trước khi gọi `updatedb.ps1`.

```powershell
.\scripts\add.ps1 AddVehicleIndex -WhatIf
.\scripts\updatedb.ps1 -WhatIf
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\run.ps1
# Chạy riêng hoặc đổi cổng:
.\scripts\run.ps1 -Only Backend
.\scripts\run.ps1 -Only Frontend
.\scripts\run.ps1 -FrontendPort 8444 -BackendPort 5081
.\scripts\run.ps1 -NoBuild
```

`run.ps1` mặc định dùng API `http://localhost:5080` và UI `http://localhost:8443`. `-NoBuild` dùng DLL Debug/net10.0 đã build, frontend vẫn chạy Vite dev. Script đặt môi trường Development và proxy frontend tới BackendPort đã chọn, không áp dụng migration. `-Only Frontend` cần backend được chạy riêng nếu muốn dùng API.

Script tái sử dụng dịch vụ đã trả 200 ở `/health/live` hoặc `/@vite/client`. Frontend tái sử dụng vẫn giữ proxy của lần khởi động cũ; cần khởi động lại nếu đổi BackendPort. Nếu cổng bị ứng dụng khác chiếm, script báo lỗi, không tự dừng ứng dụng đó. Log được giữ ở thư mục `%TEMP%/icarex-run-<id>` và in đường dẫn khi chạy. Nhấn Ctrl+C để dừng các process do script tạo; dịch vụ tái sử dụng vẫn tiếp tục. Khi cả hai đã chạy, script chỉ in URL rồi thoát. Nếu cần rebuild API đang chạy bằng `dotnet run` và gặp cảnh báo file exe bị khóa, dừng phiên API trước rồi chạy lại.

`be.ps1` vẫn hỗ trợ các action restore/build/test/run/migration/migrate/check-migration/check/integration; các script ngắn gọi chung logic này. Ví dụ:

```powershell
.\scripts\be.ps1 restore
.\scripts\be.ps1 build
.\scripts\be.ps1 test
```

## Chạy thủ công bằng dòng lệnh

Mở **terminal 1** tại root để chạy API:

```powershell
$env:ASPNETCORE_ENVIRONMENT = 'Development'
$env:ASPNETCORE_URLS = 'http://localhost:5080'
dotnet run --project src/iCareX.Web/iCareX.Web.Api/iCareX.Web.Api.csproj --no-launch-profile
```

Mở **terminal 2** tại root để chạy frontend:

```powershell
$env:ICAREX_API_URL = 'http://localhost:5080'
$env:PORT = '8443'
pnpm --dir src/iCareX.Web/iCareX.Web.Client dev
# Hoặc pnpm dev: root sẽ delegate về Client.
```

Mở http://localhost:8443/. Request `/api` được Vite proxy tới API; frontend không cần biết credentials PostgreSQL. Nhấn Ctrl+C ở mỗi terminal để dừng. Khi dùng cổng khác, đổi cả ASPNETCORE_URLS và ICAREX_API_URL cho khớp. Biến môi trường chỉ áp dụng cho terminal/process đang dùng.

Các lệnh migration/database thủ công, chạy từ root trong terminal có `ASPNETCORE_ENVIRONMENT=Development`:

```powershell
dotnet tool restore
# Kiểm tra model và liệt kê migration offline:
dotnet tool run dotnet-ef migrations has-pending-model-changes --project src/iCareX.Infrastructure --startup-project src/iCareX.Web/iCareX.Web.Api
dotnet tool run dotnet-ef migrations list --no-connect --project src/iCareX.Infrastructure --startup-project src/iCareX.Web/iCareX.Web.Api
# Bỏ --no-connect để đọc trạng thái applied/pending từ DB:
dotnet tool run dotnet-ef migrations list --project src/iCareX.Infrastructure --startup-project src/iCareX.Web/iCareX.Web.Api
# Tạo migration khi có thay đổi model:
dotnet tool run dotnet-ef migrations add AddVehicleIndex --project src/iCareX.Infrastructure --startup-project src/iCareX.Web/iCareX.Web.Api --output-dir Migrations
# Áp dụng migration lên DB đã cấu hình:
dotnet tool run dotnet-ef database update --project src/iCareX.Infrastructure --startup-project src/iCareX.Web/iCareX.Web.Api
# Kiểm tra kết nối DB, chỉ đọc:
dotnet run --project tools/iCareX.DatabaseTool -- check
```

Kiểm tra runtime từ terminal khác:

```powershell
Invoke-WebRequest http://localhost:5080/health/live -UseBasicParsing
Invoke-WebRequest http://localhost:5080/health/ready -UseBasicParsing
Invoke-WebRequest http://localhost:5080/openapi/v1.json -UseBasicParsing
```

`/health/live` xác nhận process đáp ứng; `/health/ready` kiểm tra kết nối DB, không chứng nhận schema đã đúng/đủ. OpenAPI `/openapi/v1.json` và Scalar http://localhost:5080/scalar/v1 chỉ bật trong Development. API `/api/v1/auth/me` trả 401 khi chưa đăng nhập là hành vi đúng.

## Build và test

```powershell
dotnet build iCareX.slnx -p:UseAppHost=false
dotnet test src/iCareX.UnitTests/iCareX.UnitTests.csproj --no-restore
dotnet test src/iCareX.IntegrationTests/iCareX.IntegrationTests.csproj --no-restore --filter Category=Host
pnpm typecheck
pnpm test
pnpm build
# Nhóm DB integration riêng, cần Docker:
dotnet test src/iCareX.IntegrationTests/iCareX.IntegrationTests.csproj
```

Integration database tests dùng PostgreSQL Testcontainers riêng, không dùng NAS. Nếu checkout/output cũ có DLL bị hỏng, chạy `dotnet build iCareX.slnx -t:Rebuild -p:UseAppHost=false` sau khi dừng API để tạo lại output. Backend cần đọc User Secrets và khoá Data Protection trong user profile; lỗi quyền khi chạy dưới sandbox không phải bằng chứng endpoint hoạt động. Xem [backend README](doc/.be/README.md).

Kiểm chứng ngày 06/11/2026: API khởi động tại 5080; live/ready/OpenAPI đều 200; auth/me trả 401 khi chưa login. `check.ps1 -Database` xác nhận 3 migration đã áp dụng và model không đổi; `checkdb.ps1` kết nối DB hiện có thành công. Chỉ thực hiện kiểm tra đọc trong lần này, không chạy update/provision/seed hay giao dịch người dùng.

## Frontend và hợp đồng dùng chung

Manifest, lockfile và node_modules thuộc `src/iCareX.Web/iCareX.Web.Client/`. `package.json` root chỉ cung cấp lệnh điều phối; `pnpm install` root gọi postinstall để cài frontend theo lockfile.

```powershell
pnpm install
pnpm dev
pnpm typecheck
pnpm test
pnpm build
python tools/contracts/generate_typescript.py
python tools/contracts/generate_typescript.py --check
```

DTO C# nằm ở [iCareX.Shared](src/iCareX.Shared/README.md). TypeScript được sinh tại `src/iCareX.Shared/typescript/`, frontend dùng alias `@shared/*`. Service interfaces thuộc Application; entity lưu trữ/invariant thuộc Domain; mapping thuộc Infrastructure.

Hai file frontend iCareXApi.ts và garage.ts đã phục hồi từ commit `0e1c3566`, cập nhật import Shared. Frontend đạt typecheck, 3 tests và production build. Chrome xác nhận 10 route khách, hộp đăng nhập, desktop 1440px/mobile 390px, không exception JavaScript. Chạy `pnpm dev`, mở http://localhost:8443/. Chưa kiểm thử các giao dịch có đăng nhập với database.

## Tài liệu

Bắt đầu từ [phân tích yêu cầu](doc/sumary-assigment.md), [kiến trúc](doc/structure-architecture.md), [user flow](doc/user-flow.md), [schema](doc/schema-database.md) và [workflow](doc/work-flow.md). [Rules](doc/rules.md), [playbook](doc/skill.md) và [write-spec](doc/write-spec.md) đặt trong doc. Chỉ dùng tài liệu reference làm blueprint; manifests/source hiện tại quyết định runtime.

Không commit secrets, logs, SDK, dependencies hoặc build artifacts.

Cấu hình, scripts và dev.json của Figma Make nằm ở `src/iCareX.Web/iCareX.Web.Client/.figma/make/`; Vite đọc site.json tại đây. Đã xóa `.figma` ở root vì build/chạy local không phụ thuộc các script chuyển tiếp. Khi dùng hook tự động của Figma Make, cần chọn thư mục Client làm project root. Script tự chuyển working directory về Client và build/deploy từ dist của Client. `.figaro` runtime cũng đã xóa. Chưa chạy host/deploy Figma thực tế trong môi trường local.

Git bỏ qua `.github/`, `doc/`, `stephen-skills/` ở root và đã bỏ các file từng được track trong các thư mục này khỏi index; file local vẫn giữ nguyên. `.figma/` ở root cũng được ignore nếu môi trường tạo lại; cấu hình `.figma` bên trong Client vẫn có thể track.
