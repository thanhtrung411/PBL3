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

---
*Tài liệu này sẽ được tự động cập nhật liên tục mỗi khi hoàn thành thêm một tính năng mới trong tương lai.*
