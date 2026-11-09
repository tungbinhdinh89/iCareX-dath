# Nguồn tài nguyên iCareX trong báo cáo

Ngày đối chiếu: 06/11/2026. Chưa đọc file iCareX trực tiếp qua Figma MCP do kết nối chưa được xác nhận. Không tuyên bố đây là export từ Figma.

- `icarex-lockup.tex`: tái hiện vector từ `src/iCareX.Web/iCareX.Web.Client/src/components/Layout.tsx` và các lớp `.brand`, `.brand-mark`, `.brand-title`, `.brand-subtitle` trong `src/index.css`. Giữ icon DirectionsCarOutlined, chữ iCareX kèm dấu chấm mint và tagline hiện có. Khung navy là nền sidebar. Không tự tạo logo mới.
- Icon `*.tex`: giữ nguyên SVG path/circle của component được Layout.tsx import, lấy từ gói `@mui/icons-material` 7.3.5 đang cài local; chuyển sang TikZ svg.path để PDF giữ vector. Đây là icon Material của thư viện, không phải icon tùy biến được xác minh từ Figma. Giữ bản LICENSE của gói tại `MUI-LICENSE.txt`.
- Font Be Vietnam Pro 400/600 và OFL.txt: sao chép từ `public/fonts/`; phục vụ chữ nhận diện theo giấy phép đi kèm.
- Màu từ `src/index.css`: primary #2864eb; navy #11223e; mint #60d4a6; secondary text #7b8799; canvas #f5f7fb; success #1b9e75; warning #d7921c; danger #e25a60.
- `public/favicon.svg` là hình generic của môi trường khởi tạo; không dùng làm logo iCareX vì Layout.tsx dùng nhận diện khác.
- Chưa tìm thấy ảnh runtime hoặc logo xuất riêng từ Figma trong nguồn đã kiểm tra. UI wireframe là sơ đồ thiết kế, không là ảnh chụp ứng dụng.

Cần TikZ với thư viện `svg.path`, `fontspec`; các màu ICNavy=11223E, ICMint=60D4A6, ICMuted=97A6BD. Khi thay bằng bản Figma trực tiếp, ghi file/node/version và nguồn export tại đây.
