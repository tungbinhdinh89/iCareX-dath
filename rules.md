# VehicleCare — Quy tắc kỹ thuật và nghiệp vụ

> Backend đã được chọn là ASP.NET Core 10 + PostgreSQL theo yêu cầu người dùng.
> Hướng dẫn backend cập nhật ở [doc/.be/README.md](doc/.be/README.md).
> Đăng ký thật và archive đã được chốt trong thiết kế backend; chưa có API nghiệp vụ.
> Các ghi chú chưa chọn công nghệ/chưa chốt phạm vi bên dưới là trạng thái prototype trước quyết định này.

Đọc cùng [sumary.md](./sumary.md) và [skill.md](./skill.md).
Tài liệu này bổ sung quy tắc riêng của VehicleCare, không thay thế `AGENTS.md`
hay chỉ dẫn của môi trường Make. Nếu có mâu thuẫn ảnh hưởng đến sản phẩm, nêu
rõ và xác nhận thay vì âm thầm đổi yêu cầu.

## 0. Quy trình issue, branch và pull request

Mỗi công việc phải được phân loại và theo dõi bằng ticket: **Feature** (tính
năng mới), **Enhancement** (cải tiến), **Fix** (sửa kỹ thuật) hoặc **Bug** (sửa
hành vi sai). Một ticket tương ứng một branch và một PR; không làm trực tiếp
trên `main`.

1. Tạo issue có scope, acceptance criteria, dependency, test/evidence và
   Definition of Done; đặt prefix `[Feature]`, `[Enhancement]`, `[Fix]` hoặc
   `[Bug]`.
2. Tạo branch từ `main`: `feature/<issue>-<slug>`,
   `enhancement/<issue>-<slug>`, `fix/<issue>-<slug>` hoặc
   `bug/<issue>-<slug>`.
3. Implement, test và self-review trên branch; cập nhật tài liệu/changelog
   liên quan. Chuyển issue sang In Progress.
4. Mở PR về `main`, liên kết issue bằng `Fixes #<issue>` hoặc `Refs #<issue>`,
   nêu rõ test, rủi ro và migration/deploy note. Chuyển sang In Review.
5. Agent không được tự approve, merge, bật auto-merge hoặc bypass protection.
   Phải chờ chủ repository xác nhận approve PR.
6. Sau khi được approve, chỉ merge khi có chỉ đạo rõ ràng. Sau khi merge,
   vẫn phải chờ chủ repository xác nhận trước khi đóng issue.
7. Chỉ khi có xác nhận đóng issue mới chuyển ticket/Project sang Done và ghi
   link PR, commit merge và evidence. Nếu chưa có xác nhận, giữ issue mở.

Issue mới phát sinh khác scope phải tạo ticket/branch riêng; không âm thầm mở
rộng PR đang review. Hotfix cũng phải có issue và PR, chỉ được rút gọn checklist
khi có lý do vận hành được ghi lại.

## 1. Phân biệt hiện trạng, yêu cầu và đề xuất

- **Hiện trạng:** frontend demo hai tài khoản, toàn bộ dữ liệu/phiên trong bộ nhớ.
- **Yêu cầu chưa được làm:** xe lưu trữ chỉ đọc. Không được báo là đã hỗ trợ.
- **Yêu cầu cần xác nhận phạm vi:** đăng ký trong phiên; việc bỏ đăng ký chưa
  được người dùng chấp thuận rõ ràng. Code/test chỉ cho hai tài khoản không phải
  bằng chứng người dùng đã hủy yêu cầu.
- **Đề xuất:** database, API, xác thực thật, transaction và các route ở `skill.md`.
  Chưa chọn framework, chưa có backend kết nối.
- Không thực hiện các thay đổi sản phẩm trên khi nhiệm vụ chỉ yêu cầu tài liệu.

## 2. Quy tắc kiến trúc frontend

1. Giữ React + TypeScript/Vite, MUI Community + Material Icons, React Router,
   TanStack Query, React Hook Form + Zod và Chart.js.
2. Giữ Astra Make Kit, nguồn token và font đã nối; không thêm hệ UI/chart/form
   cạnh tranh, gói trả phí hoặc dependency không cần thiết.
3. TypeScript strict; dùng type cụ thể và API công khai; không dùng `any` để né lỗi.
4. Tái sử dụng `useGarage`, component, helper và pattern hiện có. Component mới
   ưu tiên default export theo `AGENTS.md`; không đổi các named export cũ chỉ
   vì tài liệu này.
5. Logic nghiệp vụ dùng chung phải kiểm thử được, không gắn chặt vào JSX.
6. Không sửa trực tiếp dữ liệu cache; mutation phải trả dữ liệu nhất quán hoặc
   invalidate/refetch theo chiến lược đã chọn.
7. Đọc file và kiểm tra trạng thái Git trước khi sửa; không ghi đè thay đổi của
   người dùng, không dọn code ngoài phạm vi.

## 3. Giao diện, token và định dạng

- UI, validation, lỗi và thông báo nghiệp vụ bằng tiếng Việt.
- Tiền VNĐ, quãng đường km, nhiên liệu lít; ngày hiển thị `dd/MM/yyyy`.
- Ngày nghiệp vụ tính theo `Asia/Ho_Chi_Minh`; không so sánh “hôm nay” theo UTC
  hoặc timezone máy người dùng.
- Trong model/API dùng `YYYY-MM-DD` cho ngày không có giờ; không chuyển tùy
  tiện qua UTC midnight làm lệch ngày.
- Dùng `money()`, `number()`, `dateLabel()`, `today()` hoặc abstraction kế thừa
  cùng quy tắc, không nhân bản formatter khác nhau giữa các màn hình.
- CSS chung hiện là `src/index.css`; font Be Vietnam Pro 400/500/600.
  Không thêm font ngoài CSS, không tải font mới từ component.
- Styling mới phải dùng token chung cho thuộc tính thiết kế; nếu thiếu token,
  bổ sung ở nguồn chung. Các số cấu trúc như số cột/phần trăm dữ liệu không phải
  màu/khoảng cách hard-code để né hệ token.
- Giữ hierarchy navy/xanh dương, MUI theme và biểu đồ đồng bộ token.
- Dùng Material Icons hiện có, không dùng emoji làm icon.
- Không làm mất responsive, nhãn input, focus bàn phím, aria-label hoặc trạng
  thái loading/error/empty. Không dùng màu làm dấu hiệu trạng thái duy nhất.

## 4. Phiên và cách ly dữ liệu

### Prototype hiện tại

- Giữ hai kho dữ liệu tách theo `Account.id`, cache theo
  `["garage", accountId]`; preview dùng khóa riêng.
- Không thêm persistence, JWT giả, database hoặc gọi server nghiệp vụ khi chưa
  được yêu cầu.
- Reload mất phiên/thay đổi; logout không xóa kho mock. Không mô tả ngược lại.
- Guest được xem minh họa; ghi phải qua luồng đăng nhập demo.
- Chặn trên client không phải bảo mật thật; mọi mock data có thể được truy cập
  trong mã chạy trên trình duyệt.

### Khi triển khai backend thật

- Mọi đọc/ghi/lọc/xuất dữ liệu phải scope theo owner lấy từ phiên đáng tin cậy.
- Không tin `userId`/email/vehicleId/planId do client gửi để suy quyền.
- Kiểm tra owner cả bản ghi đích lẫn quan hệ; không chỉ xác thực người dùng tồn tại.
- Không cung cấp dữ liệu tài khoản đầu tiên làm preview công khai.
- Không lưu mật khẩu thô, dùng thông tin demo cho production hoặc đặt secret
  trong frontend/biến `VITE_*`.
- Chính sách persistence/phiên thật phải được chốt và cập nhật thông báo UI;
  không âm thầm thay ràng buộc “chỉ trong bộ nhớ” của prototype.

## 5. Xe và trạng thái lưu trữ

1. Xe có loại `"Ô tô"` hoặc `"Xe máy"`. ODO ban đầu phải hữu hạn, không âm.
2. Biển số so trùng sau khi bỏ ký tự không thuộc chữ/số theo quy tắc hiện tại
   (`replace(/\W/g, "").toUpperCase()`); lưu biển số hiển thị dạng viết hoa.
   Phạm vi trùng hiện là trong một tài khoản.
3. Khi sửa xe, `baseOdo` không được vượt ODO của bất kỳ bản ghi lịch sử nào.
4. Xóa xe hiện xóa cả lịch sử và kế hoạch; phải xác nhận rõ trên UI.
5. **Archive là yêu cầu chưa triển khai**, không đồng nghĩa xóa. Khi làm, giữ
   dữ liệu lịch sử và chặn tạo/sửa/xóa entry, plan, sửa/xóa xe đã archive.
   Mọi đường ghi, không chỉ nút trên trang chi tiết, phải tuân thủ.
6. Chính sách khôi phục và cách xuất hiện của xe archive trong báo cáo/nhắc hạn
   cần chốt riêng; không tự tạo ngoại lệ cho tính chỉ đọc.

## 6. Lịch sử và tính hợp lệ ODO

Áp dụng cho cả `fuel`, `service`, `odo`; không tạo ba chuỗi ODO độc lập.

1. Xe phải tồn tại và, khi có backend, thuộc đúng owner và còn hoạt động.
2. Ngày phải có thật, không ở tương lai theo ngày Việt Nam.
3. ODO hữu hạn, không âm và không dưới `baseOdo`.
4. Khi thêm/sửa bản ghi E, bỏ E cũ khỏi tập đối chiếu rồi tính:

```text
lower = max(baseOdo, ODO của mọi bản ghi có ngày < E.date)
upper = min(ODO của mọi bản ghi có ngày > E.date)
Điều kiện: lower ≤ E.odo ≤ upper
Không có bản ghi ngày sau → không có cận trên từ lịch sử
```

5. So sánh theo **ngày**, không theo giờ giả lập hoặc thứ tự nhập.
   Các bản ghi cùng ngày có thể có ODO khác nhau; vẫn phải nằm trong cận của
   các ngày trước/sau. Không bắt mọi bản ghi cùng ngày có ODO bằng nhau.
6. Cho phép sửa lịch sử khi chuỗi còn hợp lệ; không áp quy tắc “chỉ thêm ODO
   lớn hơn hiện tại” cho tất cả thao tác.
7. ODO hiện tại = `max(baseOdo, ODO của tất cả entries thuộc xe)`.
   Sửa/xóa bản ghi lớn nhất có thể giảm ODO hiện tại.
8. Sửa/xóa lịch sử phải làm dashboard, chi phí, quãng đường và hạn kế hoạch dùng
   cùng tập dữ liệu mới; không cập nhật riêng một con số trên thẻ xe.

## 7. Nhiên liệu, dịch vụ và validation đầu vào

- `fuel`: chi phí > 0, lít > 0.
- `service`: chi phí > 0, lít = 0.
- `odo`: chi phí = 0, lít = 0.
- Không chấp nhận số vô hạn/`NaN`; không parse chuỗi tiền đã định dạng như dữ
  liệu số. Form hiện chuẩn hóa trường không áp dụng về 0.
- `planId` chỉ dành cho service hoàn tất kế hoạch cùng xe/owner. Form hiện
  truyền liên kết; backend phải kiểm tra lại, không tin payload.
- Các giới hạn form hiện có phải được giữ hoặc đổi có chủ đích:

| Dữ liệu | Giới hạn hiện tại |
| --- | --- |
| Tên xe/họ tên | 2–60 ký tự sau trim |
| Biển số | 5–20 ký tự, chữ/số/chấm/gạch ngang/khoảng trắng |
| Năm xe | Số nguyên, 1980 đến năm hiện tại + 1 |
| ODO/baseOdo | 0–9.999.999 |
| Tiêu đề entry/plan | 2–100 ký tự sau trim |
| Chi phí | 0–10.000.000.000; fuel/service còn phải > 0 |
| Lít | 0–500; fuel còn phải > 0 |
| Ghi chú | Tối đa 500 ký tự |
| ODO hạn đầu | 1–9.999.999 |
| Chu kỳ km | 1–1.000.000 |
| Chu kỳ tháng | Số nguyên, 1–120 |
| Ngưỡng nhắc ngày | Số nguyên, 1–365 |
| Ngưỡng nhắc km | Số hữu hạn, 1–100.000 |
| Điện thoại | Để trống hoặc 10 chữ số bắt đầu bằng 0 sau bỏ khoảng trắng |

Đây là ràng buộc **ở các form hiện tại**, không phải tất cả đều đã được
`validateEntry()` hay mock API cưỡng chế. Khi viết backend, phải hợp nhất kiểm
tra trên server và bổ sung test, không tuyên bố mock đã an toàn tương đương.

## 8. Kế hoạch và hoàn tất bảo dưỡng

1. Form hiện yêu cầu cả ngày/ODO hạn đầu và chu kỳ tháng/km dương. Chế độ chỉ
   theo ngày hoặc chỉ theo km chưa có; không tự hiểu trường 0 là “tắt điều kiện”.
2. Hoàn tất qua **một service hợp lệ có chi phí và `planId`**, không bằng nút
   chỉ bật `completed = true`, không ghi thêm khoản chi phụ.
3. Chọn bản ghi liên kết mới nhất theo ngày giảm dần, cùng ngày theo ODO giảm
   dần. Với dữ liệu hợp lệ, đó phải là service cùng xe.
4. Chưa có service liên kết: dùng `Plan.dueDate`, `Plan.dueOdo`.
5. Có service: hạn ODO = service.odo + intervalKm; hạn ngày = ngày service cộng
   intervalMonths, chặn ngày về ngày hợp lệ cuối tháng đích.
6. Ví dụ: 31/01/2026 + 1 tháng = 28/02/2026, không tràn sang tháng 3.
7. Sửa/xóa service phải tính lại từ service liên kết mới nhất còn lại; nếu không
   còn thì quay về hạn đầu. Không giữ hạn đã cộng một cách mồ côi.
8. Sửa chu kỳ hiện làm hạn được tính lại từ service mới nhất theo chu kỳ mới;
   nếu muốn đóng băng lịch sử chu kỳ thì cần yêu cầu/model riêng.
9. Xóa plan giữ entries và chi phí dịch vụ, chỉ bỏ `planId`.

### Công thức trạng thái hiện tại

```text
km   = dueOdo - ODO hiện tại
days = chênh lệch ngày dueDate và hôm nay tại Asia/Ho_Chi_Minh

overdue: days < 0 HOẶC km < 0
soon:    chưa overdue VÀ (days ≤ thresholdDays HOẶC km ≤ thresholdKm)
good:    các trường hợp còn lại
```

Điều kiện ngày/km kết hợp **OR**, không phải AND.
Ở đúng hạn (`days = 0` hoặc `km = 0`), code hiện trả `soon`, chưa phải `overdue`.
Không tự đổi thành `<= 0` trên backend; nếu cần nhãn “đến hạn” riêng phải cập
nhật contract, UI và test đồng bộ.

## 9. Báo cáo và nhắc nhở

- Tổng chi = tổng entries fuel + service trong phạm vi lọc; ODO thuần không
  đóng góp chi phí. Không cộng lại service thông qua kế hoạch.
- Báo cáo dùng cùng owner/xe/tháng cho bảng, chỉ số và CSV. Biểu đồ xu hướng có
  thể dùng cửa sổ 6 tháng riêng như hiện tại, phải ghi nhãn đúng phạm vi.
- `driven()` hiện tính quãng đường ghi nhận: với tất cả lịch sử, mốc đầu là
  baseOdo; với một tháng, mốc đầu là ODO lớn nhất trước đầu tháng hoặc baseOdo.
  Lấy ODO lớn nhất trong kỳ trừ mốc đầu, tối thiểu 0, rồi cộng theo xe.
- Không có bản ghi trong kỳ thì quãng đường ghi nhận là 0; không suy đoán số km
  thực tế xe đã chạy, không gọi đây là đo GPS hay telemetry.
- Chi phí/km phải bảo vệ chia cho 0. Đơn giá nhiên liệu trung bình hiện là tổng
  chi nhiên liệu/tổng lít, không phải chỉ số tiêu hao lít/100 km.
- Các chỉ số trên trang nhật ký hiện theo tập cơ sở xe/tháng/loại trang, còn ô
  tìm kiếm lọc bảng; không tuyên bố mọi chỉ số đều lọc theo chuỗi tìm kiếm.
- CSV hiện xuất ở trang báo cáo, có BOM UTF-8, escape dấu nháy và ngày tiếng Việt.
  Khi chuyển server, xuất đủ dữ liệu theo bộ lọc, không chỉ trang phân trang.
  Nếu mở cho dữ liệu người dùng thật, bổ sung chống CSV formula injection.
- Tắt `settings.reminders` chỉ tắt badge/số nhắc, không đổi công thức trạng thái,
  không xóa kế hoạch và không ẩn quyền xem danh sách.
- Không hứa nhắc nền khi web đóng; không tự thêm email/SMS/push/scheduler.

## 10. Bất biến bắt buộc cho backend thật

1. Mọi quy tắc liên bản ghi phải kiểm tra trong transaction trên dữ liệu mới nhất.
2. Cùng một thao tác phải hoặc hoàn tất toàn bộ, hoặc rollback toàn bộ.
3. Request retry không được tạo trùng service/chi phí; cần idempotency phù hợp.
4. Bảo vệ trước ghi đồng thời gây sai ODO, biển số trùng hoặc cập nhật snapshot cũ.
5. Backend tự sinh ID, suy owner, kiểm tra FK, archive và field được phép sửa.
6. Không nhận callback JavaScript qua HTTP, không `eval`, không cho client ghi đè
   toàn bộ Garage để thay cho lệnh nghiệp vụ.
7. Cache hoặc dữ liệu tổng hợp không được trở thành nguồn số liệu cạnh tranh
   với entries; phải tái tính được.
8. Migration, seed, admin và job phải giữ các bất biến như API thông thường.

## 11. Quy tắc kiểm chứng và bàn giao

- Tuân thủ đúng lệnh trong `README.md`, chọn kiểm tra theo mức rủi ro như `skill.md`.
- Không khởi động thêm dev server; không sửa `.figma/make/` ngoài yêu cầu.
- Không sửa test để che regression; test phải thể hiện hành vi nghiệp vụ đúng.
- Không nói đã build/test/typecheck thành công nếu chưa chạy và nhận exit code 0.
- Với tài liệu, kiểm tra đúng ba tên `sumary.md`, `skill.md`, `rules.md`, liên kết,
  cú pháp ví dụ và tính nhất quán với mã nguồn.
- Báo rõ thay đổi, kiểm tra đã chạy, giới hạn môi trường và quyết định còn mở.
- Không tự commit, push, chuyển runtime hoặc triển khai production.
