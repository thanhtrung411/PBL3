using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using PBL3.Models;
using PBL3.Services.Interfaces;

namespace PBL3.Controllers
{
    public class BookingController : Controller
    {
        private readonly ILoaiPhongService _loaiPhongService;
        private readonly IDatPhongService _datPhongService;
        private readonly IKhachHangService _khachHangService;
        private readonly IBangGiaPhongService _bangGiaPhongService;
        private readonly IChiTietHoaDonService _chiTietHoaDonService;
        private readonly IHoaDonService _hoaDonService;

        public BookingController(
            ILoaiPhongService loaiPhongService, 
            IDatPhongService datPhongService, 
            IKhachHangService khachHangService,
            IBangGiaPhongService bangGiaPhongService,
            IChiTietHoaDonService chiTietHoaDonService,
            IHoaDonService hoaDonService)
        {
            _loaiPhongService = loaiPhongService;
            _datPhongService = datPhongService;
            _khachHangService = khachHangService;
            _bangGiaPhongService = bangGiaPhongService;
            _chiTietHoaDonService = chiTietHoaDonService;
            _hoaDonService = hoaDonService;
        }

        public async Task<IActionResult> Index()
        {
            ViewBag.LoaiPhongs = new SelectList(await _loaiPhongService.GetAllLoaiPhongsAsync(), "MaLoaiPhong", "TenLoaiPhong");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitBooking(string cccd, string hoTen, string soDienThoai, string maLoaiPhong, DateOnly ngayNhan, DateOnly ngayTra)
        {
            if (ngayNhan >= ngayTra)
            {
                TempData["Error"] = "Ngày trả phòng phải sau ngày nhận phòng.";
                return RedirectToAction(nameof(Index));
            }

            // 1. Xử lý Khách Hàng (Tạo mới hoặc cập nhật)
            var khachHang = await _khachHangService.GetByCccdAsync(cccd);
            string maKh;

            if (khachHang == null)
            {
                maKh = "KH" + new Random().Next(100000, 999999).ToString();
                var newKh = new KhachHang
                {
                    MaKh = maKh,
                    Cccd = cccd,
                    HoTen = hoTen,
                    SoDienThoai = soDienThoai
                };
                await _khachHangService.CreateAsync(newKh);
            }
            else
            {
                maKh = khachHang.MaKh;
                if (khachHang.HoTen != hoTen || khachHang.SoDienThoai != soDienThoai)
                {
                    khachHang.HoTen = hoTen;
                    khachHang.SoDienThoai = soDienThoai;
                    await _khachHangService.UpdateAsync(khachHang);
                }
            }

            // 2. Tạo Đặt Phòng
            string maDatPhong = "DP" + new Random().Next(100000, 999999).ToString();
            
            var datPhong = new DatPhong
            {
                MaDatPhong = maDatPhong,
                MaKh = maKh,
                MaNv = "NV_ONLINE", // Mã nhân viên ảo dành cho Đặt online
                TenKhSnapshot = hoTen,
                CccdSnapshot = cccd,
                SdtSnapshot = soDienThoai,
                NgayDat = DateTime.Now,
                NgayNhanPhong = ngayNhan,
                NgayTraPhong = ngayTra,
                TrangThai = "Chờ thanh toán"
            };

            // DatPhongService.CreateAsync sẽ tự động tạo một HoaDon rỗng đi kèm
            var createDpResult = await _datPhongService.CreateAsync(datPhong);
            if (!createDpResult)
            {
                 TempData["Error"] = "Lỗi hệ thống khi tạo phiếu đặt phòng. Vui lòng thử lại.";
                 return RedirectToAction(nameof(Index));
            }

            // 3. Xử lý giá cả và Chi Tiết Hóa Đơn
            decimal giaPhong = await _bangGiaPhongService.LayGiaPhongHienTaiAsync(maLoaiPhong);
            int soNgayO = ngayTra.DayNumber - ngayNhan.DayNumber;
            decimal tongTienPhong = giaPhong * soNgayO;

            // Tìm hóa đơn rỗng vừa được tạo tự động (mã HD tương ứng với DP)
            // Vì HoaDonService chưa có hàm GetByMaDatPhong, ta lấy danh sách và lọc tạm
            var hoaDons = await _hoaDonService.GetAllAsync();
            var hoaDonHienTai = hoaDons.FirstOrDefault(h => h.MaDatPhong == maDatPhong);

            if (hoaDonHienTai != null)
            {
                // Ép thanh toán 100% tiền phòng làm cọc cho Đặt online
                hoaDonHienTai.TienDatCoc = tongTienPhong;
                await _hoaDonService.UpdateAsync(hoaDonHienTai);

                // Tạo Chi Tiết Hóa Đơn (Lưu ý: Không gán MaPhong cụ thể, chỉ ghi nhận loại)
                var chiTiet = new ChiTietHoaDon
                {
                    MaCthd = "CT" + new Random().Next(100000, 999999).ToString(),
                    MaHoaDon = hoaDonHienTai.MaHoaDon,
                    LoaiMuc = "Phong",
                    // MaPhong để rỗng (null), chờ lễ tân xếp phòng thật sau
                    NoiDung = "Thuê phòng loại " + maLoaiPhong,
                    SoNguoi = 2, // Mặc định
                    SoLuong = 1, // 1 phòng
                    DonGia = giaPhong,
                    ThanhTien = tongTienPhong,
                    TrangThai = "Bình thường"
                };
                await _chiTietHoaDonService.CreateAsync(chiTiet);

                // Tính toán lại tổng tiền hóa đơn tự động
                await _hoaDonService.TinhToanTongTienAsync(hoaDonHienTai.MaHoaDon);
            }
            
            return RedirectToAction("Success", new { id = maDatPhong });
        }

        public IActionResult Success(string id)
        {
            ViewBag.MaDatPhong = id;
            return View();
        }
    }
}
