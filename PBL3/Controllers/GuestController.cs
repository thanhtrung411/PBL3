using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using PBL3.Models;
using PBL3.Services.Interfaces;

namespace PBL3.Controllers
{
    public class GuestController : Controller
    {
        // Khai báo các Service
        private readonly ILoaiPhongService _loaiPhongService;
        private readonly IBangGiaPhongService _bangGiaService;
        private readonly IDatPhongService _datPhongService;
        private readonly IKhachHangService _khachHangService;

        public GuestController(
            ILoaiPhongService loaiPhongService,
            IBangGiaPhongService bangGiaService,
            IDatPhongService datPhongService,
            IKhachHangService khachHangService)
        {
            _loaiPhongService = loaiPhongService;
            _bangGiaService = bangGiaService;
            _datPhongService = datPhongService;
            _khachHangService = khachHangService;
        }

        // ==========================================
        // HÀM GET: TRANG CHỦ (DANH SÁCH PHÒNG)
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var danhSachLoaiPhong = await _loaiPhongService.GetAllLoaiPhongsAsync();
            return View(danhSachLoaiPhong);
        }

        // ==========================================
        // HÀM GET: TRANG ĐIỀN THÔNG TIN & HÓA ĐƠN
        // ==========================================

        [HttpGet]
        public async Task<IActionResult> Checkout(string roomId, DateTime checkIn, DateTime checkOut)
        {

            var loaiPhong = await _loaiPhongService.GetLoaiPhongByIdAsync(roomId);
            if (loaiPhong == null) return NotFound();

            var giaPhong = await _bangGiaService.LayGiaPhongHienTaiAsync(roomId);

            string hinhAnhMinhHoa = "https://images.unsplash.com/photo-1611892440504-42a792e24d32?q=80&w=800&auto=format&fit=crop";
            if (loaiPhong.TenLoaiPhong != null)
            {
                if (loaiPhong.TenLoaiPhong.Contains("Standard")) hinhAnhMinhHoa = "https://images.unsplash.com/photo-1611892440504-42a792e24d32?q=80&w=800&auto=format&fit=crop";
                else if (loaiPhong.TenLoaiPhong.Contains("Deluxe")) hinhAnhMinhHoa = "https://images.unsplash.com/photo-1582719478250-c89cae4dc85b?q=80&w=800&auto=format&fit=crop";
                else if (loaiPhong.TenLoaiPhong.Contains("Suite")) hinhAnhMinhHoa = "https://images.unsplash.com/photo-1631049307264-da0ec9d70304?q=80&w=800&auto=format&fit=crop";
                else if (loaiPhong.TenLoaiPhong.Contains("Presidential") || loaiPhong.TenLoaiPhong.Contains("Tổng Thống")) hinhAnhMinhHoa = "https://images.unsplash.com/photo-1578683010236-d716f9a3f461?q=80&w=800&auto=format&fit=crop";
            }

            var model = new CheckoutViewModel
            {
                RoomId = roomId,
                RoomName = loaiPhong.TenLoaiPhong,
                ImageUrl = hinhAnhMinhHoa,
                PricePerNight = giaPhong,
                CheckIn = checkIn != default ? checkIn : DateTime.Today,
                CheckOut = checkOut != default ? checkOut : DateTime.Today.AddDays(1)
            };

            return View(model);
        }

        // ==========================================
        // HÀM POST: XỬ LÝ KHI BẤM NÚT XÁC NHẬN
        // ==========================================
        [HttpPost]
        public async Task<IActionResult> ProcessPayment(CheckoutViewModel data)
        {
            // Kiểm tra xem dữ liệu khách gửi lên có hợp lệ không
            if (!ModelState.IsValid)
            {
                return RedirectToAction("Index");
            }

            // GỢI Ý LOGIC XỬ LÝ DATABASE:
            // 1. Dùng _khachHangService kiểm tra data.PhoneNumber hoặc data.Email xem có trong hệ thống chưa, nếu chưa thì Add mới.

            // 2. Dùng _datPhongService tạo mới một đơn đặt phòng.
            // - Trạng thái: N'GIU_CHO'.
            // - Thiết lập cơ chế khóa phòng 15 phút: Voucher đặt phòng này sẽ giữ chỗ trong 15 phút để chờ khách hoàn tất thanh toán Momo/VNPay. Nếu hết thời gian mà chưa thanh toán, hệ thống hủy và nhả phòng.
            // - Lưu Snapshot: data.CustomerName, data.PhoneNumber.

            // 3. Sau khi gọi await CreateAsync(...) thành công, chuyển tới trang Success.

            return RedirectToAction("Success");
        }

        // ==========================================
        // HÀM GET: TRANG BÁO THÀNH CÔNG
        // ==========================================
        [HttpGet]
        public IActionResult Success()
        {
            return View();
        }
        public IActionResult Checkout()
        {
            // Lệnh này sẽ tự động tìm file tại /Views/Guest/Checkout.cshtml
            return View();
        }
    }
}