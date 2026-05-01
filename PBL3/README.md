# PBL3 - Hotel Management

Ứng dụng quản lý khách sạn xây dựng bằng ASP.NET Core MVC (.NET 10), Entity Framework Core và SQL Server.

## Trạng thái hiện tại

Dự án đang trong giai đoạn gộp UI và chuẩn hóa route. Một số phần là template giao diện, chưa phải nghiệp vụ hoàn chỉnh.

### Public booking template

Frontend đặt phòng online hiện đã được gom về `Booking`.

Route chuẩn:

| Route | Mục đích |
| --- | --- |
| `/` | Trang đặt phòng online |
| `/Booking` | Trang đặt phòng online |
| `/Booking/Rooms` | Trang chọn phòng mẫu |
| `/Booking/Checkout?roomId=1` | Trang checkout mẫu |
| `/Booking/Success?id=BKTEST` | Trang đặt phòng thành công |

Các route cũ của `Guest` chỉ còn dùng để redirect:

| Route cũ | Redirect sang |
| --- | --- |
| `/Guest/Index` | `/Booking` |
| `/Guest/Rooms` | `/Booking/Rooms` |
| `/Guest/Checkout?roomId=1` | `/Booking/Checkout?roomId=1` |
| `/Guest/BookingSuccess?id=...` | `/Booking/Success/...` |

Lưu ý: flow `Booking` hiện là template/mẫu frontend. Nút tìm kiếm chuyển sang trang chọn phòng mẫu, checkout tạo mã đặt phòng giả lập. Chưa lưu đặt phòng online vào DB ở flow này.

### Admin template

Khi bấm **Quản trị** trên navbar public, hệ thống đi vào dashboard/template quản trị.

Route chuẩn:

| Route | Mục đích |
| --- | --- |
| `/Admin` | Dashboard quản trị template |

Các màn hình admin template mới được merge ở root như:

- `/Home/Index`
- `/Room`
- `/BookingManagement`
- `/Customer`
- `/Invoice`
- `/Promotion`
- `/Report`
- `/Service`
- `/Facility`

đang là template/dashboard mẫu, không coi là CRUD thật hoặc nghiệp vụ đã hoàn chỉnh.

### Source CRUD

Các màn hình CRUD scaffold/nối DB thật nằm trong Area Admin nhưng được publish dưới prefix `/Source`.

| Route | Mục đích |
| --- | --- |
| `/Source` | Trang vào khu Source CRUD |
| `/Source/LoaiPhongs` | CRUD loại phòng |
| `/Source/Phongs` | CRUD phòng |
| `/Source/BangGiaPhongs` | CRUD bảng giá |
| `/Source/KhachHangs` | CRUD khách hàng |
| `/Source/DatPhongs` | CRUD đặt phòng |
| `/Source/HoaDons` | CRUD hóa đơn |
| `/Source/ChiTietHoaDons` | CRUD chi tiết hóa đơn |
| `/Source/DichVus` | CRUD dịch vụ |
| `/Source/NhanViens` | CRUD nhân viên |
| `/Source/TaiKhoans` | CRUD tài khoản |
| `/Source/VaiTros` | CRUD vai trò |
| `/Source/MaGiamGias` | CRUD mã giảm giá |

Các route `/Admin` và `/Source` đều yêu cầu đăng nhập.

## Auth

Ứng dụng dùng cookie authentication.

- Public booking không cần đăng nhập.
- Admin dashboard template cần đăng nhập.
- Source CRUD cần đăng nhập.
- Login nằm ở `/Account/Login`.
- Logout nằm ở `/Account/Logout`.

Login hiện kiểm tra bảng `TaiKhoan` trong DB. Mật khẩu hiện so sánh plain text theo dữ liệu DB hiện tại; chưa có password hashing.

## Cấu hình database

Không lưu connection string thật trong `appsettings.json`.

Dùng User Secrets khi chạy local:

```powershell
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "<connection-string>"
```

Với SQL Server remote hiện tại, máy local đang cần thêm:

```text
Encrypt=False;TrustServerCertificate=True;
```

Trên môi trường deploy, có thể dùng biến môi trường:

```powershell
$env:ConnectionStrings__DefaultConnection = "<connection-string>"
```

## Chạy dự án

Khuyến nghị chạy HTTP, không dùng launch profile `https` nếu máy local bị treo ở bước HTTPS.

```powershell
dotnet restore
dotnet build --no-restore
dotnet run --no-build --no-launch-profile --urls http://localhost:5199
```

Mở:

```text
http://localhost:5199
```

hoặc:

```text
http://localhost:5199/Booking
```

## Ranh giới cần giữ khi phát triển tiếp

1. `BookingController` là nơi sở hữu frontend đặt phòng online.
2. `GuestController` chỉ giữ redirect để tương thích link cũ, không thêm UI mới vào `Views/Guest`.
3. `/Admin` là dashboard/template quản trị.
4. `/Source` là khu CRUD scaffold/nối DB thật, code vẫn nằm trong `Areas/Admin`.
5. Khi nối booking online với DB thật, nên tách service riêng như `OnlineBookingService`, không nhét thêm logic lớn vào controller.
6. Cần sửa encoding tiếng Việt toàn dự án trước khi hoàn thiện nghiệp vụ, đặc biệt trong `.cshtml`, `DomainValues.cs` và các thông báo lỗi.

## Việc cần làm tiếp

- Dọn admin root template và Area Admin để không trùng khái niệm.
- Chuẩn hóa tên route admin.
- Sửa encoding tiếng Việt.
- Tách nghiệp vụ đặt phòng online thật khỏi controller.
- Bổ sung model/view model cho booking search, room selection và checkout.
- Thêm test route cơ bản cho public booking, auth và admin CRUD.
