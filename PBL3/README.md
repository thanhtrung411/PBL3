# PBL3 - Hệ Thống Quản Lý Khách Sạn (Hotel Management System)

Dự án phát triển phần mềm quản lý khách sạn, được xây dựng trên nền tảng **ASP.NET Core MVC (.NET 10)**, sử dụng **Entity Framework Core** và cơ sở dữ liệu **SQL Server**. Hệ thống cung cấp các chức năng đặt phòng trực tuyến cho khách hàng và bộ công cụ quản trị toàn diện cho nhân viên/quản lý.

## 🚀 Tính Năng Nổi Bật

### 1. Dành Cho Khách Hàng (Public Booking)
- **Tìm kiếm và đặt phòng trực tuyến:** Khách hàng có thể dễ dàng tìm kiếm phòng trống theo ngày và loại phòng.
- **Thanh toán trực tuyến:** Tích hợp cổng thanh toán **VNPay**.
- **Thông báo Email:** Tự động gửi email xác nhận đặt phòng, thông báo hóa đơn đến khách hàng.
- **Không yêu cầu tài khoản:** Khách hàng có thể đặt phòng nhanh chóng mà không bắt buộc phải đăng ký tài khoản.

### 2. Dành Cho Quản Trị Viên & Nhân Viên (Admin Dashboard)
- **Bảng điều khiển (Dashboard):** Thống kê trực quan doanh thu, số lượng phòng được đặt, tình trạng phòng hiện tại.
- **Quản lý Đặt phòng & Nhận/Trả phòng (Lễ tân):** Cập nhật trạng thái đặt phòng, hỗ trợ check-in/check-out cho khách nhanh chóng.
- **Quản lý Phòng & Loại phòng:** Thêm, sửa, xóa thông tin phòng, cập nhật bảng giá phòng linh hoạt theo thời điểm.
- **Quản lý Hóa đơn & Dịch vụ:** Ghi nhận các dịch vụ phát sinh, áp dụng mã giảm giá (Promotion) và xuất hóa đơn chi tiết.
- **Quản lý Nhân sự & Khách hàng:** Quản lý tài khoản nhân viên, phân quyền truy cập (Admin, Lễ tân,...), lưu trữ và quản lý thông tin khách hàng.
- **Báo cáo Thống kê:** Báo cáo doanh thu chi tiết theo thời gian.

### 3. Hệ Thống Xử Lý Ngầm (Background Services)
- Tự động dọn dẹp các đặt phòng đã hết hạn nhưng chưa thanh toán (`ExpiredBookingCleanupHostedService`).
- Kiểm tra và đánh dấu các trường hợp quá hạn trả phòng (`OverdueCheckoutWorker`).

## 🛠 Công Nghệ Sử Dụng

- **Framework:** ASP.NET Core MVC (.NET 10)
- **ORM:** Entity Framework Core 10
- **Database:** SQL Server
- **Authentication:** Cookie-based Authentication với cơ chế Password Hashing an toàn.
- **Thanh toán:** Tích hợp VNPay API.
- **Gửi Email:** SMTP Email Sender.
- **Tiện ích khác:** Sinh mã QR với thư viện QRCoder.

## 📂 Cấu Trúc Phân Hệ

Hệ thống được thiết kế chia thành các phân hệ rõ ràng:
1. **Public/Guest Area (`/Booking`):** Giao diện dành cho khách hàng thao tác đặt phòng trực tuyến. Mọi luồng xử lý từ trang chủ đến lúc thanh toán đều tập trung ở đây. *(Các route `/Guest` cũ tự động redirect sang `/Booking`)*.
2. **Admin Area (`/Admin`):** Khu vực nghiệp vụ dành cho nhân viên và quản lý. Yêu cầu đăng nhập.
3. **Source CRUD (`/Source`):** Khu vực thao tác trực tiếp với Database (được sinh ra qua Scaffold) dành riêng cho quản trị viên cấp cao để quản trị dữ liệu thô.

## ⚙️ Cài Đặt & Chạy Dự Án

### Yêu Cầu Hệ Thống
- .NET 10 SDK
- SQL Server (Local hoặc Remote)

### Bước 1: Cấu hình Môi trường (.env)
Dự án sử dụng file `.env` để quản lý chuỗi kết nối Database và các thiết lập bảo mật khác.

Bạn cần tạo một file `.env` ngang hàng với file `PBL3.csproj` (có thể copy từ file `.env.example` sang `.env`) và cập nhật thông tin kết nối SQL Server của bạn:
```env
ConnectionStrings__DefaultConnection=Server=YOUR_SERVER_NAME;Database=PBL3;Trusted_Connection=True;Encrypt=False;TrustServerCertificate=True;
# Bạn cũng có thể cấu hình thông số VNPay và SMTP (Email) tại file này.
```

### Bước 2: Build dự án
```powershell
dotnet restore
dotnet build
```

### Bước 3: Khởi tạo Database
Dự án này sử dụng phương pháp **Database-First** nên không có sẵn EF Migrations. Để tạo cơ sở dữ liệu, bạn cần chạy script SQL đính kèm:
1. Mở SQL Server Management Studio (SSMS) hoặc phần mềm quản lý tương đương.
2. Mở và chạy file script `.sql` của dự án (Lưu ý: Bạn cần export Database của bạn ra một file `.sql` và đính kèm vào source code để người khác có thể chạy).
3. Đảm bảo tên Database được cấu hình trong file `.env` khớp với tên Database vừa tạo.

*(Hệ thống có cơ chế tự động Warm-up DB và mã hóa mật khẩu dạng plain-text ở lần chạy đầu tiên).*

### Bước 4: Chạy ứng dụng
Khuyến nghị chạy ứng dụng qua giao thức HTTP (để tránh lỗi chứng chỉ SSL ở môi trường dev):
```powershell
dotnet run --no-launch-profile --urls http://localhost:5199
```

Truy cập hệ thống:
- Giao diện khách hàng: `http://localhost:5199`
- Giao diện quản trị: `http://localhost:5199/Admin`

## 🔐 Cơ Chế Đăng Nhập
- Hệ thống sử dụng Cookie Authentication.
- Đường dẫn đăng nhập: `/Account/Login`
- Luồng bảo mật: Khi khởi chạy hệ thống, background task sẽ tự động dò tìm các mật khẩu dạng plain text trong Database và tiến hành băm (Hash) mật khẩu để bảo mật cho các lần đăng nhập tiếp theo.

## 📝 Định Hướng Phát Triển Tiếp Theo
- Hoàn thiện tách biệt logic nghiệp vụ khỏi các Controller thành các Service độc lập (như `OnlineBookingService`).
- Dọn dẹp hoàn toàn Admin root template và Source CRUD để tối ưu trải nghiệm quản trị.
- Sửa lỗi font chữ, encoding tiếng Việt còn tồn đọng trong một số view `.cshtml` và thông báo hệ thống.
- Bổ sung Unit Test và Integration Test cho các tính năng trọng yếu.

---
*Dự án PBL3 - Đồ án cơ sở ngành*
