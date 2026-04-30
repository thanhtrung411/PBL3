# Dự án Quản lý Khách sạn (PBL3)

## 📌 Tổng quan
Hệ thống quản lý khách sạn được xây dựng trên nền tảng ASP.NET Core MVC (C# 14, .NET 10). Dự án áp dụng chuẩn mô hình kiến trúc 3 lớp (3-Layer Architecture) để tách biệt logic nghiệp vụ, quản lý truy xuất dữ liệu, và giao diện người dùng.

## 🚀 Tiến độ dự án (Changelog)

### ✅ Nền tảng kiến trúc & CRUD Cơ bản
- Tổ chức dự án theo mô hình 3 lớp: `Models` - `Services` - `Controllers`.
- Hoàn thiện các luồng CRUD (Thêm, Sửa, Xóa, Xem chi tiết) và Giao diện cơ bản (Razor Pages) cho toàn bộ các thực thể:
  - **Cốt lõi**: Đặt phòng (`DatPhong`), Hóa đơn (`HoaDon`), Chi tiết hóa đơn (`ChiTietHoaDon`).
  - **Phòng ốc**: Phòng, Loại phòng, Bảng giá phòng, Dịch vụ.
  - **Khách và Nhân sự**: Khách hàng, Nhân viên, Tài khoản, Vai trò.
  - **Khuyến mãi**: Mã giảm giá.

### ✅ Giai đoạn 1: Logic Nghiệp vụ Đặt phòng & Hóa đơn
- **Dịch vụ Đặt Phòng (`DatPhongService`)**:
  - Tích hợp thuật toán `KiemTraPhongTrongAsync` chống trùng phòng (Overlap validation), đảm bảo an toàn tuyệt đối không có 2 khách đặt cùng 1 phòng trong cùng khoảng thời gian.
  - Snapshot tự động thông tin Khách hàng (Tên, SĐT, CCCD) tại thời điểm đặt phòng.
  - Sử dụng Database Transaction để tự động sinh `HoaDon` rỗng đi kèm khi khởi tạo một `DatPhong` mới.
- **Dịch vụ Hóa Đơn (`HoaDonService`)**:
  - Hàm `TinhToanTongTienAsync` tự động tính toán tổng tiền: Phân loại rạch ròi Tiền Phòng và Tiền Dịch Vụ dựa trên `ChiTietHoaDon`.
  - Tự động kiểm tra thời hạn và áp dụng `MaGiamGiaPhong` (Giảm theo % hoặc số tiền cứng, có kiểm tra giới hạn GiamToiDa).
- **Dịch vụ Phòng (`PhongService`)**:
  - Hỗ trợ hàm `CapNhatTrangThaiPhongAsync` phục vụ cho các luồng nghiệp vụ Check-in (Đổi thành "Đang sử dụng") và Check-out (Đổi thành "Cần dọn dẹp").

### ✅ Giao diện Đặt phòng Online (BookingController)
- **Luồng dành cho Khách hàng (Landing Page)**: Khách tự do tra cứu và đặt "Loại Phòng" mà không cần chọn phòng cụ thể.
- **Tự động hóa dữ liệu**: 
  - Khách chỉ nhập CCCD, hệ thống tự động tra cứu, tạo mới Khách Hàng (Tự sinh mã `KH...`) hoặc cập nhật thông tin nếu khách đổi SĐT.
  - Tự động gán phiếu cho nhân viên ảo `NV_ONLINE`.
- **API Tra cứu (AJAX)**: Cung cấp endpoint `[HttpGet] /KhachHangs/GetByCccd` để phục vụ Auto-fill không chạm.
- **Chính sách cọc**: Code đã được thiết kế ép cọc 100% tiền phòng cho đơn hàng Online.

### ✅ Giao diện Quản trị (Admin Dashboard)
- **Hub Trung tâm (`/Admin`)**: Khởi tạo trang Dashboard cơ bản quản lý toàn bộ hệ thống khách sạn.
- **Admin Layout (`_AdminLayout.cshtml`)**: Tách biệt giao diện của Khách và Quản trị viên. Tích hợp thanh điều hướng bên trái (Sidebar) kết nối tới toàn bộ 12 luồng CRUD của Database (Phòng, Khách hàng, Đặt phòng, Nhân sự,...).

---
*Tài liệu này sẽ được tự động cập nhật liên tục mỗi khi hoàn thành thêm một tính năng mới trong tương lai.*

## Cấu hình bảo mật

Không lưu connection string thật trong `appsettings.json`. Sau khi đổi mật khẩu database, cấu hình local bằng User Secrets:

```powershell
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "<connection-string-moi>"
```

Trên môi trường deploy, dùng biến môi trường:

```powershell
$env:ConnectionStrings__DefaultConnection = "<connection-string-moi>"
```

## Các chỉnh sửa gần đây

- **Bảo mật cấu hình DB**: Gỡ connection string thật khỏi `appsettings.json`, bật `UserSecretsId` trong project và yêu cầu cấu hình `ConnectionStrings:DefaultConnection` bằng User Secrets hoặc biến môi trường.
- **Chuẩn hóa trạng thái theo DB**: Thêm `DomainValues` để dùng thống nhất các giá trị như `GIU_CHO`, `CHUA_THANH_TOAN`, `PHONG`, `DICHVU`, `HIEU_LUC`, tránh lệch với CHECK constraint trong SQL Server.
- **Luồng đặt phòng online an toàn hơn**: Bọc quy trình tạo khách hàng, đặt phòng, hóa đơn và chi tiết hóa đơn trong transaction; nếu bước sau lỗi thì không commit dữ liệu dở dang.
- **Sinh mã an toàn bằng DB sequence**: Thêm `CodeGenerator` lấy số từ SQL Server sequence để tránh trùng mã khi có nhiều request đồng thời, ví dụ `KH00000001`, `DP00000001`, `CT00000001`, và hóa đơn dạng `H000000001`.
- **Nhân viên đặt phòng online**: Booking tự đảm bảo có nhân viên hệ thống `NV_ONLINE` trước khi tạo `DatPhong`, tránh lỗi khóa ngoại `FK_DatPhong_NhanVien`.
- **Fix lỗi nullable trong CTHD**: Các trang `ChiTietHoaDons/Index` và `Details` hiển thị `-` khi không có `MaPhong` hoặc `MaDv`, tránh `NullReferenceException`.
- **Tối ưu truy cập DB lần đầu**: Đổi sang `AddDbContextPool`, thêm warm-up DB khi app khởi động và dùng `AsNoTracking()` cho các truy vấn chỉ đọc trong service.
- **Chặn booking giá 0**: Luồng đặt phòng online kiểm tra bảng giá active trước khi tạo dữ liệu; nếu loại phòng chưa có giá thì báo lỗi và không tạo khách hàng, đặt phòng, hóa đơn hoặc chi tiết hóa đơn.
- **Chuẩn hóa xử lý lỗi service/controller**: Các controller Admin kiểm tra kết quả `CreateAsync`, `UpdateAsync`, `DeleteAsync` trước khi redirect; nếu thất bại thì hiển thị lỗi qua `ModelState` hoặc `TempData`. Các service xóa dữ liệu chỉ bắt `DbUpdateException`, không còn `catch` trống nuốt mọi lỗi.
- **Sửa lỗi tạo hóa đơn Admin**: `HoaDonsController.Create` chỉ bắt lỗi DB dự kiến và báo đúng hơn khi đặt phòng không tồn tại hoặc đã có hóa đơn, tránh thông báo sai cho mọi exception.
- **Tách layout Public/Admin**: Thêm `Areas/Admin/Views/Shared/_AdminLayout.cshtml` với sidebar quản trị riêng; layout public chỉ giữ điều hướng khách và link vào trang quản trị.
- **Rà soát View**: Sửa form trang chủ trỏ nhầm `SearchController` chưa tồn tại, bỏ header public bị trùng trong trang khách, đổi icon Booking sang Font Awesome, bỏ alert lỗi trùng và sửa link về trang chủ bằng tag helper.
