---
name: frontend-design
description: Thiết kế và chỉnh sửa UI iCareX theo bộ design system xanh lá/Inter, logo do owner cung cấp, React/MUI và UX tiếng Việt.
---

## Chuẩn iCareX — đọc trước

Trong repository iCareX, đọc `doc/rules.md` và `doc/guidelines/Guidelines.md`.
Nguồn thị giác là `Design/Design_system_1.png` (component/trạng thái) và
`Design/Design_system_2.png` (brand/tokens/responsive), do owner chỉ định.
Các chuẩn sau ưu tiên hơn lời khuyên sáng tạo tổng quát bên dưới và ví dụ
thương hiệu mặc định của Astra:

- Font **Inter** hỗ trợ tiếng Việt, self-host tại `public/fonts/inter-variable.ttf`;
  không tự đổi sang Be Vietnam Pro/Instrument Sans hoặc tránh Inter.
- Canvas `#F4F8F1`, surface `#FFFFFF`, text `#14263E` / `#586A80`,
  border `#DCE3ED`, navigation `#1F4D3A`, action/focus `#2F6B4A`.
- Dùng đúng vector `public/brand/brand-logo-{light,dark}.svg` và
  `brand-symbol-{light,dark}.svg`; tên biến thể là nền sử dụng.
  Không vẽ lại logo bằng chữ/icon ô tô hoặc dùng AstraLogo.
- Desktop: sidebar xanh **224px** + topbar; tablet: rail **72px**, giữ nhãn
  truy cập; mobile: drawer **320px**, topbar **64–72px**, nội dung một cột.
- Nhịp 4/8/12/16/24/32; page inset desktop 32, card padding 24, form gap 24;
  radius input/button 6, card 10, presentation panel 16; vùng chạm ≥44px.
- Typography: heading 32–40/650, title 24/650, subtitle 18/600,
  body 16/400/1.5, table/control 14, caption 12.
- Status có chữ và ký hiệu: Chưa đến hạn, Sắp đến hạn, Đến hạn, Quá hạn,
  Không đủ dữ liệu. Không bịa trạng thái hoặc dữ liệu khi API/model chưa có.
- App/home được bảo vệ bởi phiên server. Chờ `/auth/me` trước khi render;
  guest vào `/dang-nhap`, không nháy dashboard mẫu. Có phiên thì chỉ hiện
  tài khoản thật, không nút “Đăng nhập”. Logout/401 phải bỏ cache dữ liệu riêng.
- Giữ React 19/MUI Community, form/state/API thật; font và token wiring ở
  `src/index.css`, bridge MUI ở `src/lib/theme/theme.ts`. Giữ setup Astra
  khi cần, không áp palette tím/icon rail 60px của package lên chuẩn iCareX.
- Kiểm 1440/768/360px; mobile history dùng ActivityCard; kiểm loading/empty/
  error/disabled/pending/focus. Chạy typecheck, tests, build và xem ảnh trình duyệt.

## Hướng dẫn thiết kế tổng quát

Kỹ năng này hướng dẫn tạo giao diện người dùng có dấu ấn riêng, sẵn sàng dùng trong thực tế. Khi có design system do người dùng chọn, độ chính xác và tính nhất quán với system đó là ưu tiên.

Người dùng cung cấp yêu cầu về thành phần giao diện, trang web, ứng dụng hoặc giao diện cần xây dựng. Họ cũng có thể cung cấp bối cảnh về mục đích, đối tượng sử dụng hoặc các ràng buộc kỹ thuật.

## Tư duy thiết kế

Trước khi viết mã, hiểu bối cảnh và chọn một định hướng thẩm mỹ TÁO BẠO:
- **Mục đích**: Giao diện này giải quyết vấn đề gì? Ai sử dụng?
- **Phong cách**: Chọn một hướng thể hiện rõ nét: tối giản triệt để, hỗn loạn theo chủ nghĩa tối đa, vị lai hoài cổ, hữu cơ/tự nhiên, sang trọng/tinh tế, vui nhộn/như đồ chơi, biên tập/tạp chí, thô mộc/nguyên bản, nghệ thuật trang trí/hình học, dịu nhẹ/màu phấn, công nghiệp/thực dụng, v.v. Có rất nhiều phong cách để lựa chọn. Dùng chúng làm cảm hứng nhưng phải thiết kế nhất quán với định hướng đã chọn.
- **Ràng buộc**: Các yêu cầu kỹ thuật như khung phát triển, hiệu năng và khả năng tiếp cận.
- **Điểm khác biệt**: Điều gì khiến giao diện này KHÓ QUÊN? Đâu là chi tiết mà người xem sẽ nhớ?

**CỐT YẾU**: Chọn ý tưởng chủ đạo rõ ràng và thực hiện chính xác. Cả phong cách tối đa táo bạo lẫn tối giản tinh tế đều hiệu quả; điều quyết định là chủ đích thiết kế, không phải mức độ phô trương.

Sau đó triển khai mã hoạt động được (HTML/CSS/JS, React, Vue, v.v.) đáp ứng các tiêu chí:
- Sẵn sàng dùng trong thực tế và đầy đủ chức năng.
- Nổi bật về thị giác và dễ ghi nhớ.
- Nhất quán với một quan điểm thẩm mỹ rõ ràng.
- Được trau chuốt tỉ mỉ trong từng chi tiết.

## Nguyên tắc thẩm mỹ giao diện

Tập trung vào:
- **Kiểu chữ**: Dùng font và thang chữ của design system đã được chọn. Chỉ đề xuất font mới khi chưa có quy định; iCareX dùng Inter.
- **Màu sắc và chủ đề**: Theo đuổi phong cách thẩm mỹ nhất quán. Dùng biến CSS để đảm bảo tính đồng bộ. Màu chủ đạo rõ ràng với điểm nhấn sắc nét hiệu quả hơn bảng màu dè dặt, phân bố đều.
- **Chuyển động**: Dùng hoạt ảnh cho hiệu ứng và các tương tác nhỏ. Với HTML, ưu tiên giải pháp chỉ dùng CSS. Với React, dùng thư viện Motion khi có sẵn. Tập trung vào những thời điểm tạo ấn tượng mạnh: một hiệu ứng tải trang được phối hợp tốt, với các phần xuất hiện lần lượt (animation-delay), mang lại nhiều hứng thú hơn các tương tác nhỏ rải rác. Dùng hiệu ứng kích hoạt khi cuộn và trạng thái khi rê chuột để tạo bất ngờ.
- **Bố cục không gian**: Bố cục bất ngờ. Bất đối xứng. Chồng lớp. Luồng thị giác theo đường chéo. Thành phần phá vỡ lưới. Khoảng trống rộng rãi HOẶC mật độ được kiểm soát.
- **Nền và chi tiết thị giác**: Tạo không khí và chiều sâu thay vì mặc định dùng màu nền đơn sắc. Thêm hiệu ứng và chất liệu phù hợp với bối cảnh và thẩm mỹ tổng thể. Áp dụng sáng tạo các dạng lưới chuyển sắc, chất liệu nhiễu, họa tiết hình học, lớp trong suốt chồng nhau, bóng đổ ấn tượng, đường viền trang trí, con trỏ tùy chỉnh và lớp phủ hạt.

Tránh phối màu và bố cục tùy tiện, thiếu cá tính riêng của bối cảnh. Không diễn giải lời khuyên này thành lý do thay font, logo hay tokens đã được người dùng chỉ định.

Diễn giải sáng tạo và đưa ra những lựa chọn bất ngờ, cho thấy thiết kế được đầu tư riêng cho bối cảnh. Các thiết kế không nên giống hệt nhau. Thay đổi giữa chủ đề sáng và tối, các phông chữ và phong cách thẩm mỹ khác nhau. TUYỆT ĐỐI KHÔNG lặp lại những lựa chọn quen thuộc (chẳng hạn Space Grotesk) qua nhiều lần tạo.

**QUAN TRỌNG**: Điều chỉnh độ phức tạp của phần triển khai cho phù hợp với định hướng thẩm mỹ. Thiết kế theo chủ nghĩa tối đa cần mã công phu, nhiều hoạt ảnh và hiệu ứng. Thiết kế tối giản hoặc tinh tế cần sự tiết chế, chính xác và chú ý kỹ đến khoảng cách, kiểu chữ cùng các chi tiết nhỏ. Sự thanh lịch đến từ việc thực hiện tốt định hướng đã chọn.

Hãy nhớ: Cursor có khả năng tạo ra những sản phẩm sáng tạo xuất sắc. Hãy mạnh dạn thể hiện những gì có thể tạo ra khi suy nghĩ vượt khuôn mẫu và theo đuổi trọn vẹn một định hướng riêng biệt.
