# Kiểm tra báo cáo ngày 07/11/2026 — bổ sung chương giao diện

Trạng thái: đã hoàn thiện chương 5 và bìa bằng ảnh thiết kế do người dùng cung cấp.
Phương án dùng ảnh được người dùng cho phép sau khi kiểm tra lại truy cập Figma.

Cập nhật bìa: toàn bộ dòng giảng viên in đậm, cỡ 14 pt; tên ghi PHAN TRUNG HIẾU.
Đã biên dịch lại, kiểm tra tài liệu và xem riêng ảnh bìa mới; cả ba lệnh mã thoát 0.

Cập nhật trình bày: bìa ghi Đồ Án Tổng Hợp; footer là iCareX: Vehicle Management.
Số trang hiển thị liên tục 1--90 sau bìa, dùng chữ số tự nhiên. Tiêu đề danh mục
bảng đổi thành Mục lục. Đã đối chiếu văn bản PDF và xem các trang chịu ảnh hưởng.

## Đầu ra

- Nguồn chính: [main.tex](main.tex).
- Nội dung giao diện: [chapters/05-interface-demo.tex](chapters/05-interface-demo.tex).
- PDF: [dist/main.pdf](dist/main.pdf), bản sao [main.pdf](main.pdf).
- 91 trang, 1.854.727 byte; SHA-256 của PDF mới:
  FD359B86DCDE2353ABADDC753FE953EBB5DECE03148B83E7FCA79C78C18F6C6D.
- Chương 5 ở trang in 70--83, tương ứng trang PDF 71--84.
- Giữ sáu chương và tài liệu tham khảo; 24 thẻ use case, 53 ca kiểm thử dự kiến.
- Nhánh docs/11-revise-report, liên quan issue #11; không commit, push hoặc đóng issue.

## Kiểm tra truy cập Figma

Cấu hình local xác nhận server figma được bật, dùng Streamable HTTP tại
https://mcp.figma.com/mcp và hỗ trợ OAuth. Trong phiên này không có công cụ
Figma để gọi đọc file; công cụ web cũng không lấy được nội dung từ link thiết kế.
Không coi việc liệt kê cấu hình là một lần đọc file thành công.

Tệp nguồn: dhuYBdCuQxXFoNb6OpOjdW, node 35:451.
Người dùng đã cho phép dùng hai ảnh thiết kế và sáu ảnh trang trong Design làm
nguồn thay thế. Chương 5 không lấy logo, icon hoặc token từ source client.

## Nội dung bổ sung

- Nguyên lý thiết kế, phân cấp thông tin và phản hồi.
- Logo hai nền và tám icon điều hướng; bìa dùng logo trích từ ảnh nền tảng.
- Bảng màu, Inter, nhịp 4 px, bo góc, mức nổi và vòng focus.
- Năm trạng thái bảo dưỡng và các trạng thái biểu mẫu/danh sách.
- Responsive ở các kích thước tham chiếu 1440, 768 và 360 px.
- Sáu trang: giới thiệu, đăng nhập, đăng ký, chi tiết xe, ghi nhiên liệu, hộp thư.
- Kịch bản demo theo dữ liệu Minh Anh/Yamaha NVX 155:
  12.699 km → nhiên liệu 12.700 km → đến hạn 13.000 km → bảo dưỡng.
- Các bảng được viết bằng LaTeX để chỉnh sửa; hình giao diện giữ nguyên nguồn ảnh.

## Nguồn ảnh và giới hạn

35 tài nguyên được chuẩn bị; 29 tài nguyên xuất hiện trong PDF. Các trang được
sao chép nguyên byte. Logo/icon và vùng minh họa thành phần được cắt đúng pixel,
không sửa nội dung, không đổi màu và không vẽ lại.
[evidence/design-assets.json](evidence/design-assets.json) lưu nguồn, kích thước,
tọa độ và SHA-256. Ảnh gốc trong Design không bị sửa.

Ba trang có vùng trong suốt được đặt trên nền F4F8F1 trong tài liệu, đúng bảng
nền tảng thiết kế. Không sửa PNG để thực hiện việc này.

Ảnh đăng nhập vẫn có dòng hướng dẫn tải trang cần đăng nhập lại; ảnh đăng ký
vẫn ghi mật khẩu tối thiểu 8 ký tự. Phần mô tả trong báo cáo áp dụng DEC07
(cookie HttpOnly, session DB, CSRF, khôi phục phiên hợp lệ) và chính sách mật khẩu
tối thiểu 12 ký tự. Ảnh chỉ dùng minh họa bố cục, không thay đặc tả tài khoản.
Không khẳng định đã kiểm thử tương tác hoặc native prototype trên Figma.

## Kết quả thực tế

| Kiểm tra | Kết quả |
| --- | --- |
| Chuẩn bị tài nguyên bằng script | Mã thoát 0; 35 tài nguyên có nguồn |
| LuaLaTeX qua latexmk, TeX Live 2026, Biber | Mã thoát 0 |
| Kiểm tra tài liệu và nguồn ảnh | Mã thoát 0; 29 tài nguyên dùng đều khớp SHA-256 |
| 6 trang minh họa bắt buộc | Có đủ |
| Nội dung bìa, thành viên và DEC07 | Có đủ |
| Các mục đã yêu cầu bỏ, Wiki/JWT, hình tự dựng cũ trong báo cáo | Không còn |
| Citation/reference không phân giải | 0 |
| Font warning, missing glyph, Overfull/Underfull | 0 |
| Chữ ra ngoài vùng lề an toàn | 0 |
| Mã TC giữa bảng và CSV | Khớp 53 ca |
| Xuất ảnh xem trước | Mã thoát 0; 91 trang, 8 contact sheet |
| Xem bố cục | Đã xem bìa, toàn chương 5, các trang sau chương 5 và trang tiếp giáp |
| git diff --check | Mã thoát 0; có thông báo LF/CRLF của Git |
| Unit/host/integration/frontend | Không chạy; chỉ sửa tài liệu và chuẩn bị ảnh |
| Đọc trực tiếp file bằng Figma MCP | Chưa thực hiện được; đã dùng phương án ảnh được cho phép |

## Lệnh tái lập

    powershell -NoProfile -ExecutionPolicy Bypass -File report_template_latex/scripts/prepare_design_assets.ps1
    powershell -NoProfile -ExecutionPolicy Bypass -File report_template_latex/build.ps1
    python -X utf8 report_template_latex/scripts/check_report.py
    powershell -NoProfile -ExecutionPolicy Bypass -File report_template_latex/scripts/render_preview.ps1

## Log và ảnh xem trước

Lần build cuối:
[console.log](logs/main/20261107T021430.6914478Z-build/console.log) và
[diagnostics](logs/main/20261107T021430.6914478Z-build/diagnostics).

[Log kiểm tra](logs/main/20261107T021430.6914478Z-build/verification.log),
[log xuất ảnh](logs/main/20261107T015744.2944648Z-build/render-preview.log) và
[kết quả JSON](evidence/document-validation.json).

Ảnh ở [rendered/20261107T015926.1309921Z](rendered/20261107T015926.1309921Z).
Ảnh bìa mới: [cover.png](rendered/cover-20261107T020545/cover.png),
[log xuất bìa](logs/main/20261107T020545.5486067Z-build/render-cover.log).
Các log và ảnh của những lần trước tiếp tục được giữ nguyên.

Đối chiếu footer, số trang, tên môn và tên danh mục:
[kết quả](evidence/page-numbering-validation.json),
[log](logs/main/20261107T021430.6914478Z-build/formatting-verification.log).
Ảnh cập nhật tại [footer-20261107T021430](rendered/footer-20261107T021430),
[log xuất ảnh](logs/main/20261107T021430.6914478Z-build/render-selected.log).
Lần kiểm tra trước đã phát hiện tên danh mục bị Babel ghi đè; bản sửa dùng
caption tiếng Việt và đã đạt đối chiếu. Kết quả ban đầu được giữ trong log cũ.

