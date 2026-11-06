# iCareX.Shared

Hợp đồng dữ liệu dùng chung cho frontend, API và service backend.

- `Contracts/Identity`: DTO đăng ký, đăng nhập, tài khoản và CSRF.
- `Contracts/Vehicles`: DTO xe, catalog, ODO và version.
- `typescript`: type frontend sinh từ các DTO C#, truy cập bằng alias `@shared/*`.

Application giữ service interfaces và lỗi nghiệp vụ; Shared chỉ chứa hợp đồng trao đổi không phụ thuộc EF/HTTP/Identity. Entity lưu trữ và invariant thuộc Domain, mapping thuộc Infrastructure. Model trao đổi mới được thêm vào Shared khi thực sự cần cho contract frontend/backend.

Sau khi sửa positional record DTO, chạy từ root:

```powershell
python tools/contracts/generate_typescript.py
python tools/contracts/generate_typescript.py --check
```

Generator hỗ trợ các kiểu đang dùng: string, Guid/date/timestamp, bool, số, nullable và collection. Kiểu chưa hỗ trợ phải bổ sung mapping có chủ đích; `--check` báo lỗi khi contract TypeScript lệch nguồn C#.
