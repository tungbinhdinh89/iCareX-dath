# Ghi chú thuyết trình iCareX — Nhóm 2

Thời lượng gợi ý: 12–15 phút cho 17 slide. Đây là lời dẫn để tập nói;
slide chỉ giữ những ý chính và hình cần cho người nghe theo dõi.

## 1. Giới thiệu đề tài — 40 giây

Kính chào thầy và các bạn. Nhóm 2 xin trình bày iCareX, ứng dụng web quản lý
phương tiện và nhắc lịch bảo dưỡng. Nhóm gồm Nguyễn Thanh Tùng, MSSV 2449106;
Ngô Minh Triết, MSSV 2313562; và Nông Thế Vinh, MSSV 2115302, dưới sự hướng dẫn
của thầy Phan Trung Hiếu.

Bài trình bày đi từ nhu cầu của chủ xe đến giải pháp, kiến trúc và các luồng
sử dụng trên web. Mục tiêu là giúp người dùng biết xe đã sử dụng ra sao,
đã chi bao nhiêu và công việc nào cần theo dõi tiếp.

## 2. Bối cảnh và mục tiêu — 50 giây

Khi sử dụng xe, chúng ta có thể lưu hóa đơn ở một nơi, ghi số km ở nơi khác
và nhớ lịch bảo dưỡng bằng trí nhớ. Khi cần xem lại, rất khó nối các thông
tin này thành một lịch sử đầy đủ. Ngay cả khi nhớ được lần thay dầu gần nhất,
người dùng vẫn cần đối chiếu với số km hiện tại để biết kỳ tiếp theo.

iCareX tập trung các dữ liệu đó theo từng xe. Nhóm muốn giải quyết ba câu hỏi:
xe đã chi bao nhiêu, đã làm những gì và khi nào cần bảo dưỡng tiếp.

## 3. Người dùng và hành trình — 45 giây

Đối tượng chính là chủ xe máy và ô tô cá nhân, sử dụng hệ thống qua trình duyệt
web. Một tài khoản có thể theo dõi nhiều phương tiện; mỗi hoạt động được gắn
với đúng chiếc xe để lịch sử và chi phí không bị lẫn.

Hành trình bắt đầu bằng thêm xe, sau đó ghi hoạt động, theo dõi hạn bảo dưỡng,
ghi nhận công việc đã làm và xem lại lịch sử. Điểm quan trọng là bước cuối
trở thành dữ liệu đầu vào cho kỳ theo dõi tiếp theo.

## 4. Các nhóm chức năng — 50 giây

Ứng dụng có bốn nhóm chức năng. Hồ sơ phương tiện quản lý thông tin xe và
số km. Kế hoạch bảo dưỡng xác định công việc theo ngày, số km hoặc điều kiện
đến trước. Nhật ký ghi nhiên liệu, sửa chữa và các hạng mục bảo dưỡng.
Lịch sử và báo cáo giúp người dùng lọc theo xe, thời gian và xem khoản đã ghi.

Thay vì đi qua từng màn hình riêng lẻ, phần sau sẽ dùng cùng một chiếc xe
để cho thấy bốn nhóm này liên kết với nhau như thế nào.

## 5. Logic nhắc bảo dưỡng — 60 giây

Sơ đồ chia trách nhiệm thành chủ xe, hệ thống và thông báo. Khi số km thay đổi,
hệ thống đối chiếu với kế hoạch. Với kế hoạch theo thời gian, ngày hiện tại
cũng là căn cứ để đánh giá hạn. Nếu chưa đủ điều kiện nhắc, hệ thống tiếp tục
theo dõi; khi đủ điều kiện, nhắc được tạo hoặc cập nhật trong inbox.

Người dùng mở nhắc để xem lý do và quyết định xử lý. Sau khi ghi nhận một lần
bảo dưỡng, lịch sử thay đổi và kỳ tiếp theo cần được đánh giá lại. Đây là
logic nghiệp vụ đích; sơ đồ không thay cho kết quả kiểm thử worker.

## 6. Thiết kế giao diện web — 45 giây

Đây là trang chi tiết phương tiện từ bộ thiết kế iCareX. Điều hướng nằm bên
trái, nội dung chính ở giữa; các thao tác ghi hoạt động được đặt trong ngữ
cảnh của xe đang xem. Số km, chi phí và các kế hoạch bảo dưỡng xuất hiện cùng
một trang để người dùng dễ đối chiếu.

Trạng thái không chỉ dựa vào màu: tên trạng thái, mốc đến hạn và khoảng còn
lại đều cần đọc được. Phạm vi trình bày lần này là ứng dụng web.

## 7. Kiến trúc hệ thống — 60 giây

Theo luồng chạy, React SPA nhận thao tác từ trình duyệt, gửi dữ liệu đến
ASP.NET Core API và nhận kết quả để hiển thị. API xử lý quyền truy cập,
kiểm tra dữ liệu và thực hiện nghiệp vụ; PostgreSQL lưu trữ dữ liệu bền vững.

Theo tổ chức mã nguồn, Domain giữ mô hình và quy tắc, Application giữ interface
và hợp đồng nội bộ, Infrastructure triển khai dịch vụ và truy cập dữ liệu.
Ba khối phía trên mô tả luồng chạy; ba phần bên dưới mô tả trách nhiệm trong
mã nguồn, giúp việc thay đổi giao diện hoặc lưu trữ ít ảnh hưởng đến quy tắc.

## 8. Công nghệ sử dụng — 50 giây

Frontend dùng React 19, TypeScript và Vite 8; Tailwind CSS 4, MUI và Astra UI
phục vụ giao diện. Backend dùng ASP.NET Core 10 và Identity. Dữ liệu được lưu
bằng PostgreSQL, truy cập qua EF Core và Npgsql. Nhóm dùng xUnit và
Testcontainers cho các kiểm tra tương ứng.

Một điểm kết nối giữa hai phía là Shared DTO. Từ hợp đồng C#, nhóm sinh
TypeScript để giao diện và API có cùng định nghĩa cấu trúc dữ liệu.

## 9. Triển khai web — 45 giây

Phương án triển khai cho người dùng là truy cập qua HTTPS. Web server hoặc
reverse proxy phục vụ giao diện và chuyển tiếp đến API cùng origin;
PostgreSQL được giữ trong mạng riêng. Dữ liệu, khóa bảo vệ phiên và bản sao
lưu cần tồn tại bền vững ngoài gói ứng dụng.

Trong môi trường phát triển, Vite và API chạy riêng, dùng proxy để giao tiếp.
Slide này mô tả phương án kiến trúc triển khai, không khẳng định đã có
môi trường production vận hành.

## 10. Bảo vệ tài khoản và dữ liệu — 60 giây

Nhóm chú trọng ba nguyên tắc. Thứ nhất, dữ liệu phải thuộc đúng tài khoản:
phiên dùng cookie HttpOnly, lưu trong DB và có CSRF cho thao tác ghi.
Thứ hai, lịch sử số km phải phù hợp theo thời gian; sửa một hoạt động cũ
có thể ảnh hưởng đến tổng chi phí và kỳ bảo dưỡng liên quan.

Thứ ba, thao tác ghi cần tránh trùng và tránh ghi đè thay đổi của người khác.
Thiết kế sử dụng transaction cho dữ liệu liên quan, RequestId khi gửi lại
và version khi cập nhật. Đây cũng là những tình huống cần kiểm chứng khi
hoàn thiện các luồng nghiệp vụ.

## 11. User flow 01: theo dõi phương tiện — 50 giây

Sau khi đăng nhập, người dùng chọn Yamaha NVX 155 để mở trang tổng quan.
Trong ví dụ ngày 01/10, xe đang ở 12.699 km và chưa có chi phí tháng 10.
Kế hoạch thay dầu đến hạn ở 13.000 km, ngưỡng nhắc trước là 300 km.

Như vậy xe còn 301 km, chưa vào vùng sắp đến hạn. Trên cùng trang, người dùng
có thể xem thông tin hoặc tiếp tục ghi nhiên liệu, chi phí và số km.
Các hình user flow minh họa hành vi từ thiết kế đã cung cấp.

## 12. User flow 02: ghi nhiên liệu — 55 giây

Từ trang xe, người dùng chọn Ghi nhiên liệu. Ví dụ ngày 02/10, họ nhập
12.700 km, 8 lít và 200.000 VNĐ. Sau khi lưu, hệ thống cần phản ánh cùng
một hoạt động ở lịch sử, tổng chi phí và số km hiện tại.

Khoảng còn lại đến kỳ thay dầu lúc này là 300 km, đúng bằng ngưỡng nhắc.
Vì vậy trạng thái chuyển sang sắp đến hạn. Có thể nhấn mạnh rằng một lần
nhập liệu cung cấp thông tin cho cả nhật ký và việc theo dõi bảo dưỡng.

## 13. User flow 03: xử lý nhắc bảo dưỡng — 60 giây

Đến ngày 15/10, xe đạt 13.000 km. Phần lý do cho thấy mốc trước 10.000 km
cộng chu kỳ 3.000 km tạo ra mốc đến hạn 13.000 km. Người dùng có thể hiểu
ngay tại sao mình nhận được nhắc này.

Có hai hành động khác nhau. Đánh dấu đã đọc giảm số nhắc chưa đọc nhưng không
đổi kỳ bảo dưỡng. Ghi nhận bảo dưỡng mở dịch vụ được liên kết với xe và kế
hoạch, từ đó mới có căn cứ tính kỳ mới sau khi công việc thực sự được ghi.

## 14. User flow 04: hoàn thành và chuyển kỳ — 55 giây

Ngày 16/10, dịch vụ được ghi tại 13.000 km, gồm sửa chữa 100.000 VNĐ và
bảo dưỡng 150.000 VNĐ. Hạng mục bảo dưỡng liên kết với kế hoạch thay dầu,
giúp mốc tiếp theo được tính thành 16.000 km.

Tổng tháng là 450.000 VNĐ: 200.000 nhiên liệu cộng 250.000 dịch vụ.
Mỗi khoản chỉ được tính một lần. Nếu sửa lịch sử dịch vụ, các số tổng và
mốc bảo dưỡng liên quan cần được cập nhật lại để giữ nhất quán.

## 15. Phạm vi và mức độ hoàn thiện — 50 giây

Nền tảng mã nguồn hiện có gồm tài khoản, phiên, danh mục phương tiện, quản lý
xe và ODO thủ công. Những luồng cần tiếp tục hoàn thiện là nhiên liệu,
dịch vụ, kế hoạch, nhắc hạn, inbox và báo cáo.

Bốn user flow vừa trình bày thể hiện hành vi thiết kế hướng tới. Mức hoàn
thiện cần được đối chiếu với API, luồng chạy và dữ liệu lưu thực tế.
Trọng tâm phiên bản này là chủ xe cá nhân sử dụng giao diện web tiếng Việt.

## 16. Lộ trình hoàn thiện — 45 giây

Nhóm triển khai theo ba bước. Đầu tiên kết nối giao diện với API và hoàn
thiện các liên kết nghiệp vụ. Tiếp theo kiểm thử xuyên suốt, đặc biệt là
quyền truy cập giữa hai tài khoản, sửa lịch sử, gửi lại và cập nhật đồng thời.
Cuối cùng là đóng gói ứng dụng web, kiểm chứng luồng và ghi nhận kết quả.

Tiêu chí chung là dữ liệu đúng, thao tác dễ hiểu và truy vết được từ hoạt
động đã ghi đến tổng chi phí cũng như kỳ bảo dưỡng tiếp theo.

## 17. Lời cảm ơn — 10 giây

Nhóm xin cảm ơn thầy và các bạn đã lắng nghe!
