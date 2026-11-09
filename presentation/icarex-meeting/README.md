# Thuyết trình iCareX — ứng dụng web

Mở [iCareX-ThuyetTrinh.pdf](iCareX-ThuyetTrinh.pdf) để trình chiếu: 17 slide,
tỷ lệ 16:9. [Ghi chú thuyết trình](Ghi-chu-thuyet-trinh.md) có lời dẫn riêng
theo từng trang, gợi ý cho bài nói khoảng 12–15 phút.

Tham chiếu bố cục: [Vehicle_Maintenance_Management_System.pdf](../Vehicle_Maintenance_Management_System.pdf).
Giữ cách chia bài toán/giải pháp, các thẻ chức năng, sơ đồ nghiệp vụ và kiến trúc;
nội dung, công nghệ và hình giao diện được điều chỉnh theo iCareX trên web.
Tiêu đề xanh dương, điểm nhấn xanh lá, nền trắng, font Be Vietnam Pro được nhúng
trong PDF. Văn bản và sơ đồ có thể sửa trong [main.tex](main.tex).

## Nội dung

1. Giới thiệu đề tài, giảng viên và thành viên.
2. Bối cảnh và mục tiêu giải pháp.
3. Người dùng và hành trình nghiệp vụ.
4. Bốn nhóm chức năng cốt lõi.
5. Logic nhắc bảo dưỡng.
6. Thiết kế giao diện web.
7. Kiến trúc hệ thống.
8. Công nghệ sử dụng.
9. Phương án triển khai web.
10. Bảo vệ tài khoản và dữ liệu.
11. User flow 01: theo dõi phương tiện.
12. User flow 02: ghi nhiên liệu.
13. User flow 03: xử lý nhắc bảo dưỡng.
14. User flow 04: hoàn thành và chuyển kỳ.
15. Phạm vi và mức độ hoàn thiện.
16. Lộ trình hoàn thiện.
17. Lời cảm ơn.

Thông tin nhóm: Nguyễn Thanh Tùng — 2449106; Ngô Minh Triết — 2313562;
Nông Thế Vinh — 2115302. Giảng viên hướng dẫn: Phan Trung Hiếu.

Các user flow dùng ảnh thiết kế người dùng cung cấp, không phải bằng chứng
chạy ứng dụng mới. Slide phạm vi phân biệt nền tảng mã nguồn hiện có với
các luồng cần hoàn thiện. Không đưa hướng dẫn chuẩn bị môi trường vào bài nói.

## Build và kiểm tra

Chạy từ root repository, cần TeX Live 2026, LuaLaTeX, latexmk, Poppler và Python:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File presentation/icarex-meeting/build.ps1
python -X utf8 presentation/icarex-meeting/scripts/check_presentation.py
powershell -NoProfile -ExecutionPolicy Bypass -File presentation/icarex-meeting/scripts/render_preview.ps1
```

Build lưu toàn bộ output và native diagnostics riêng trong `logs/main/<run-id>/`.
PDF được đồng bộ tại `iCareX-ThuyetTrinh.pdf`, `main.pdf` và `dist/main.pdf`.
Kết quả kiểm tra: [validation.json](validation.json), [bằng chứng build và xem PDF](BUILD-VALIDATION.md). Ảnh từng trang và các
contact sheet được lưu theo từng lần render trong `rendered/`.

Nếu cập nhật tài nguyên gốc, chạy `scripts/prepare_assets.ps1` trước khi build.
Ảnh được sao chép/cắt chính xác; font lấy từ bộ font của frontend trong repo.
[assets-manifest.json](assets-manifest.json) lưu nguồn, thao tác và SHA-256.

## Phạm vi công việc

Loại `docs`, tiếp tục nhánh `docs/11-revise-report` hiện có, liên quan #11.
Tiêu chí: trình bày cho web; đủ tên/MSSV; không còn slide chuẩn bị demo hoặc
nhãn Demo 1–4; bốn user flow rõ ràng; trang cuối chỉ có lời cảm ơn; font nhúng,
không mất dấu, tràn chữ hoặc sai tỷ lệ ảnh. Ghi chú phải khớp 17 trang.

Chỉ thay đổi bộ thuyết trình. Không cần chạy build/test ứng dụng cho thay đổi
tài liệu này. Không commit, push hoặc thay đổi issue/PR trong lần chỉnh sửa.
