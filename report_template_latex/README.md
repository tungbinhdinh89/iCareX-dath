# Template báo cáo LaTeX

Giữ bố cục bìa, logo Bách Khoa, header/footer, font, lề và toàn bộ package của báo cáo gốc. Người thực hiện: Nguyễn Thanh Tùng, MSSV 2449106.

## Sử dụng

1. Sao chép thư mục này cho mỗi báo cáo mới.
2. Sửa thông tin môn học, loại báo cáo, tên đề tài, phụ đề, ngày và footer trong metadata.tex.
3. Viết nội dung vào sections/content.tex; có thể tạo thêm file và dùng lệnh \input.
4. Thêm hình vào figures/, bảng vào tables/ khi cần.
5. Bật danh sách hình, danh sách bảng và tài liệu tham khảo trong main.tex khi sử dụng. Thêm nguồn vào references.tex.

preamble.tex chứa toàn bộ package và định dạng; cover.tex chứa bố cục bìa. Ngày mặc định tự cập nhật theo ngày biên dịch.

## Biên dịch

Cần TeX Live, pdfLaTeX, latexmk và các package trong preamble.tex.

Chạy tại thư mục template:
    powershell -NoProfile -ExecutionPolicy Bypass -File .\build.ps1

PDF kết quả: dist/main.pdf. Log biên dịch: logs/main/.

Overleaf: tải bộ nguồn lên, chọn main.tex và compiler pdfLaTeX.
