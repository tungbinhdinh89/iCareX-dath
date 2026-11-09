# Kiểm tra bộ thuyết trình web — 07/11/2026

PDF: `iCareX-ThuyetTrinh.pdf`, 17 trang, 16:9, 489.832 byte.
SHA-256: `f2682d4ab4b0d5023531b0f6f4f57406d3fbf9a68e801eacd3301b1e492943fe`.

## Lệnh và kết quả

Chạy từ root repository:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File presentation/icarex-meeting/build.ps1
python -X utf8 presentation/icarex-meeting/scripts/check_presentation.py
powershell -NoProfile -ExecutionPolicy Bypass -File presentation/icarex-meeting/scripts/render_preview.ps1 -Scale 1600
```

- Build: **Passed**, exit code 0. TeX Live 2026, LuaHBTeX 1.24.0, latexmk 4.88.
- Kiểm tra PDF/nội dung/tài nguyên: **Passed**, exit code 0.
- Render: **Passed**, exit code 0; đủ 17 trang ở chiều rộng 1600 px.
- Đã xem contact sheet toàn bộ bài, kiểm tra riêng bìa, user flow và lời cảm ơn.
- Không có Overfull, thiếu ký tự, lỗi TeX, cảnh báo font hoặc chữ ngoài trang.
- Be Vietnam Pro Regular/SemiBold được nhúng; ký hiệu mũi tên dùng TeX Gyre Heros.
- Ba họ tên đi kèm đúng MSSV trên bìa; trang cuối chỉ chứa lời cảm ơn.
- Bốn nhãn USER FLOW thay nhãn Demo; không còn phần chuẩn bị chạy demo hay mobile.
- 14 tài nguyên ảnh/font đúng hash; các bản sao PDF đồng nhất; 17 mục lời dẫn khớp số trang.
- Build/test ứng dụng: **Not run**, thay đổi chỉ liên quan tài liệu thuyết trình.

## Bằng chứng

- [Kết quả kiểm tra chi tiết](validation.json).
- [Log build cuối](logs/main/20261107T114726.7733944Z-build/console.log).
- [Native diagnostics](logs/main/20261107T114726.7733944Z-build/diagnostics/).
- [Ảnh render cuối](rendered/20261107T114736.9012165Z/).
- Log trước khi sửa: `logs/main/20261107T113104.2054312Z-build/console.log`.

Các lần build thử và lỗi trung gian được giữ riêng trong `logs/main/`, không
ghi đè. Bản nguồn trước khi trình bày lại được giữ trong thư mục
`logs/source-before-web-redesign-*`. File PDF tham chiếu được giữ nguyên.
Không commit/push hoặc thay đổi trạng thái issue/PR.
