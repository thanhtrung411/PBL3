using Microsoft.AspNetCore.Mvc;

namespace PBL3.Controllers
{
    public class GuestController : Controller
    {
        // 1. GET: /Guest/Index (Trang chủ & Danh sách phòng)
        public IActionResult Index()
        {
            // Tương lai: Lấy danh sách phòng từ Database truyền ra View
            // var rooms = _context.Rooms.Where(r => r.IsAvailable).ToList();
            // return View(rooms);

            return View();
        }

        // 2. GET: /Guest/Checkout?roomId=xxx (Trang điền thông tin thanh toán)
        [HttpGet]
        public IActionResult Checkout(int roomId)
        {
            // Tương lai: Dựa vào roomId để lấy thông tin phòng (Tên, Giá, Hình ảnh) từ Database
            // var room = _context.Rooms.Find(roomId);
            // if (room == null) return NotFound();
            // ViewBag.Room = room;

            // Hiện tại cứ trả về View giao diện tĩnh trước
            return View();
        }

        // 3. POST: /Guest/ProcessPayment (Xử lý khi khách bấm nút "Xác nhận đặt phòng")
        [HttpPost]
        public IActionResult ProcessPayment(string FirstName, string LastName, string Email, string PhoneNumber, string Note)
        {
            /* --- LOGIC XỬ LÝ DATABASE SẼ VIẾT Ở ĐÂY ---
             * 1. Kiểm tra phòng còn trống không?
             * 2. Tạo bản ghi Customer mới lưu vào Database (Tên, Email, SĐT).
             * 3. Tạo bản ghi Booking mới với trạng thái "Chờ thanh toán".
             * 4. Tính toán tổng tiền (Giá phòng + Phí dịch vụ + VAT).
             */

            // Sau khi lưu Data thành công, giả sử chuyển khách sang trang Thông báo thành công
            return RedirectToAction("BookingSuccess");
        }

        // 4. GET: /Guest/BookingSuccess (Trang báo đặt phòng thành công)
        public IActionResult BookingSuccess()
        {
            return View();
        }
    }
}