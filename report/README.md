# Báo cáo đặc tả iCareX — Nhóm 2

Nguồn chính: [main.tex](main.tex). Báo cáo gồm sáu chương và tài liệu tham khảo.
Bìa ghi giảng viên hướng dẫn Phan Trung Hiếu và ba thành viên nhóm.
MSSV của Ngô Minh Triết là 2313562 và của Nông Thế Vinh là 2115302.

DEC07 dùng cookie HttpOnly, phiên lưu trong DB và CSRF. Các use case và 53
ca kiểm thử được thống nhất theo quyết định này.

## Xây dựng và kiểm tra

Dùng TeX Live 2026, LuaLaTeX, latexmk và Biber:

    powershell -NoProfile -ExecutionPolicy Bypass -File report_template_latex/build.ps1
    python -X utf8 report_template_latex/scripts/check_report.py
    powershell -NoProfile -ExecutionPolicy Bypass -File report_template_latex/scripts/render_preview.ps1

PDF ở [dist/main.pdf](dist/main.pdf), có bản sao tại [main.pdf](main.pdf).
Mỗi lần build lưu toàn bộ output và native log trong logs/main/.
Kết quả kiểm tra được ghi tại [BUILD-VALIDATION.md](BUILD-VALIDATION.md).

## Nguồn giao diện

[Tệp Figma do người dùng cung cấp](https://www.figma.com/design/dhuYBdCuQxXFoNb6OpOjdW/?node-id=35-451).

Figma MCP được cấu hình và bật nhưng phiên chat chưa có công cụ đọc file;
lần mở link bằng công cụ web cũng chưa lấy được nội dung. Người dùng đã cho phép
dùng các ảnh trong Design làm nguồn thay thế. Chương 5 sử dụng đúng bộ ảnh này
cho logo, icon, màu sắc, chữ, khoảng cách, thành phần và sáu trang minh họa.
Các hình tự dựng cũ và token suy từ client tiếp tục được loại khỏi báo cáo.

Tài nguyên dùng trong báo cáo nằm tại figures/design/. Logo, icon và các vùng
minh họa được cắt đúng pixel từ ảnh gốc; các trang được sao chép nguyên byte.
Nguồn, tọa độ và SHA-256 được lưu trong evidence/design-assets.json.
Muốn tạo lại tài nguyên sau khi cập nhật ảnh gốc:

    powershell -NoProfile -ExecutionPolicy Bypass -File report_template_latex/scripts/prepare_design_assets.ps1

Ảnh gốc không bị sửa. Dòng hướng dẫn tải lại trang trên ảnh đăng nhập và yêu cầu
8 ký tự trên ảnh đăng ký thuộc bản mẫu; phần mô tả báo cáo áp dụng DEC07 và
chính sách mật khẩu tối thiểu 12 ký tự. Logo trường của template được giữ lại.

Những file cũ của các mục đã loại bỏ được giữ nguyên trên đĩa để bảo toàn
nội dung trước đó, nhưng không được main.tex đưa vào báo cáo.
Trình kiểm tra chỉ duyệt các nguồn thực sự được báo cáo sử dụng.

## Kiểm thử phần mềm

Các ca nghiệp vụ trong evidence/test-cases.csv là kế hoạch, chưa có kết quả
thực thi mới. Công việc này chỉ chỉnh sửa và kiểm tra tài liệu.
