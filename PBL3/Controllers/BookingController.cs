using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using PBL3.Data;
using PBL3.Models;
using PBL3.Services;
using PBL3.Services.Interfaces;

namespace PBL3.Controllers
{
    [AllowAnonymous]
    public class BookingController : Controller
    {
        private const string OnlineEmployeeId = "NV_ONLINE";

        private readonly ILoaiPhongService _loaiPhongService;
        private readonly IDatPhongService _datPhongService;
        private readonly IKhachHangService _khachHangService;
        private readonly IBangGiaPhongService _bangGiaPhongService;
        private readonly IChiTietHoaDonService _chiTietHoaDonService;
        private readonly IHoaDonService _hoaDonService;
        private readonly ApplicationDbContext _context;

        public BookingController(
            ILoaiPhongService loaiPhongService, 
            IDatPhongService datPhongService, 
            IKhachHangService khachHangService,
            IBangGiaPhongService bangGiaPhongService,
            IChiTietHoaDonService chiTietHoaDonService,
            IHoaDonService hoaDonService,
            ApplicationDbContext context)
        {
            _loaiPhongService = loaiPhongService;
            _datPhongService = datPhongService;
            _khachHangService = khachHangService;
            _bangGiaPhongService = bangGiaPhongService;
            _chiTietHoaDonService = chiTietHoaDonService;
            _hoaDonService = hoaDonService;
            _context = context;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Rooms()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> Checkout(string? roomId, DateTime? checkIn, DateTime? checkOut)
        {
            var normalizedCheckIn = checkIn ?? DateTime.Today;
            var normalizedCheckOut = checkOut ?? normalizedCheckIn.AddDays(2);
            if (normalizedCheckOut <= normalizedCheckIn)
            {
                normalizedCheckOut = normalizedCheckIn.AddDays(1);
            }

            var model = await BuildCheckoutViewModelAsync(
                string.IsNullOrWhiteSpace(roomId) ? "1" : roomId,
                normalizedCheckIn,
                normalizedCheckOut);

            return View(model);
        }

        [HttpPost]
        public IActionResult ProcessPayment(CheckoutViewModel data)
        {
            if (!ModelState.IsValid)
            {
                return RedirectToAction(nameof(Index));
            }

            var bookingCode = $"BK{DateTime.Now:yyyyMMddHHmmss}";
            return RedirectToAction(nameof(Success), new { id = bookingCode });
        }

        [NonAction]
        public async Task<IActionResult> SubmitDatabaseBookingAsync(string cccd, string hoTen, string soDienThoai, string maLoaiPhong, DateOnly ngayNhan, DateOnly ngayTra)
        {
            if (ngayNhan >= ngayTra)
            {
                TempData["Error"] = "Ngày trả phòng phải sau ngày nhận phòng.";
                return RedirectToAction(nameof(Index));
            }

            decimal giaPhong = await _bangGiaPhongService.LayGiaPhongHienTaiAsync(maLoaiPhong);
            if (giaPhong <= 0)
            {
                TempData["Error"] = "Chưa cấu hình giá phòng cho loại phòng này. Vui lòng chọn loại phòng khác hoặc liên hệ lễ tân.";
                return RedirectToAction(nameof(Index));
            }

            await using var bookingTransaction = await _context.Database.BeginTransactionAsync();

            var ensureOnlineEmployeeResult = await EnsureOnlineEmployeeAsync();
            if (!ensureOnlineEmployeeResult)
            {
                TempData["Error"] = "Không thể chuẩn bị nhân viên xử lý đặt phòng online. Vui lòng thử lại.";
                return RedirectToAction(nameof(Index));
            }

            // 1. Xử lý Khách Hàng (Tạo mới hoặc cập nhật)
            var khachHang = await _khachHangService.GetByCccdAsync(cccd);
            string maKh;

            if (khachHang == null)
            {
                var newMaKh = await CodeGenerator.GenerateFromSequenceAsync(
                    _context,
                    "dbo.Seq_KhachHang",
                    "KH",
                    8);
                if (newMaKh == null)
                {
                    TempData["Error"] = "Không thể tạo mã khách hàng. Vui lòng thử lại.";
                    return RedirectToAction(nameof(Index));
                }

                maKh = newMaKh;
                var newKh = new KhachHang
                {
                    MaKh = maKh,
                    Cccd = cccd,
                    HoTen = hoTen,
                    SoDienThoai = soDienThoai
                };
                var createKhResult = await _khachHangService.CreateAsync(newKh);
                if (!createKhResult)
                {
                    TempData["Error"] = "Không thể tạo thông tin khách hàng. Vui lòng thử lại.";
                    return RedirectToAction(nameof(Index));
                }
            }
            else
            {
                maKh = khachHang.MaKh;
                if (khachHang.HoTen != hoTen || khachHang.SoDienThoai != soDienThoai)
                {
                    khachHang.HoTen = hoTen;
                    khachHang.SoDienThoai = soDienThoai;
                    var updateKhResult = await _khachHangService.UpdateAsync(khachHang);
                    if (!updateKhResult)
                    {
                        TempData["Error"] = "Không thể cập nhật thông tin khách hàng. Vui lòng thử lại.";
                        return RedirectToAction(nameof(Index));
                    }
                }
            }

            // 2. Tạo Đặt Phòng
            string? maDatPhong = null;

            maDatPhong = await CodeGenerator.GenerateFromSequenceAsync(
                _context,
                "dbo.Seq_DatPhong",
                "DP",
                8);
            if (maDatPhong == null)
            {
                TempData["Error"] = "Không thể tạo mã đặt phòng. Vui lòng thử lại.";
                return RedirectToAction(nameof(Index));
            }

            var datPhong = new DatPhong
            {
                MaDatPhong = maDatPhong,
                MaKh = maKh,
                MaNv = OnlineEmployeeId, // Mã nhân viên ảo dành cho Đặt online
                TenKhSnapshot = hoTen,
                CccdSnapshot = cccd,
                SdtSnapshot = soDienThoai,
                NgayDat = DateTime.Now,
                NgayNhanPhong = ngayNhan,
                NgayTraPhong = ngayTra,
                TrangThai = DomainValues.DatPhongTrangThai.GiuCho
            };

            // DatPhongService.CreateAsync sẽ tự động tạo một HoaDon rỗng đi kèm
            var createDpResult = await _datPhongService.CreateAsync(datPhong);
            if (!createDpResult)
            {
                 TempData["Error"] = "Lỗi hệ thống khi tạo phiếu đặt phòng. Vui lòng thử lại.";
                 return RedirectToAction(nameof(Index));
            }

            // 3. Xử lý giá cả và Chi Tiết Hóa Đơn
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
                var updateHoaDonResult = await _hoaDonService.UpdateAsync(hoaDonHienTai);
                if (!updateHoaDonResult)
                {
                    TempData["Error"] = "Không thể cập nhật hóa đơn. Vui lòng liên hệ lễ tân để kiểm tra lại đặt phòng.";
                    return RedirectToAction(nameof(Index));
                }

                var maCthd = await CodeGenerator.GenerateFromSequenceAsync(
                    _context,
                    "dbo.Seq_ChiTietHoaDon",
                    "CT",
                    8);
                if (maCthd == null)
                {
                    TempData["Error"] = "Không thể tạo mã chi tiết hóa đơn. Vui lòng liên hệ lễ tân để kiểm tra lại đặt phòng.";
                    return RedirectToAction(nameof(Index));
                }

                // Tạo Chi Tiết Hóa Đơn (Lưu ý: Không gán MaPhong cụ thể, chỉ ghi nhận loại)
                var chiTiet = new ChiTietHoaDon
                {
                    MaCthd = maCthd,
                    MaHoaDon = hoaDonHienTai.MaHoaDon,
                    LoaiMuc = DomainValues.ChiTietHoaDonLoaiMuc.Phong,
                    // MaPhong để rỗng (null), chờ lễ tân xếp phòng thật sau
                    NoiDung = "Thuê phòng loại " + maLoaiPhong,
                    SoNguoi = 2, // Mặc định
                    SoLuong = 1, // 1 phòng
                    DonGia = giaPhong,
                    ThanhTien = tongTienPhong,
                    TrangThai = DomainValues.ChiTietHoaDonTrangThai.HieuLuc
                };
                var createChiTietResult = await _chiTietHoaDonService.CreateAsync(chiTiet);
                if (!createChiTietResult)
                {
                    TempData["Error"] = "Không thể tạo chi tiết hóa đơn. Vui lòng liên hệ lễ tân để kiểm tra lại đặt phòng.";
                    return RedirectToAction(nameof(Index));
                }

                // Tính toán lại tổng tiền hóa đơn tự động
                var tinhTienResult = await _hoaDonService.TinhToanTongTienAsync(hoaDonHienTai.MaHoaDon);
                if (!tinhTienResult)
                {
                    TempData["Error"] = "Không thể tính tổng tiền hóa đơn. Vui lòng liên hệ lễ tân để kiểm tra lại đặt phòng.";
                    return RedirectToAction(nameof(Index));
                }
            }
            else
            {
                TempData["Error"] = "Không tìm thấy hóa đơn vừa tạo. Vui lòng liên hệ lễ tân để kiểm tra lại đặt phòng.";
                return RedirectToAction(nameof(Index));
            }
            
            await bookingTransaction.CommitAsync();
            return RedirectToAction("Success", new { id = maDatPhong });
        }

        public IActionResult Success(string? id)
        {
            ViewBag.MaDatPhong = id;
            return View();
        }

        private async Task<CheckoutViewModel> BuildCheckoutViewModelAsync(string roomId, DateTime checkIn, DateTime checkOut)
        {
            var roomName = "Deluxe Hướng Biển";
            var imageUrl = "https://images.unsplash.com/photo-1611892440504-42a792e24d32?q=80&w=800&auto=format&fit=crop";
            var pricePerNight = 1_200_000m;

            var loaiPhong = await _loaiPhongService.GetLoaiPhongByIdAsync(roomId);
            if (loaiPhong != null)
            {
                roomName = loaiPhong.TenLoaiPhong;
                imageUrl = GetRoomImage(roomName);

                var configuredPrice = await _bangGiaPhongService.LayGiaPhongHienTaiAsync(roomId);
                if (configuredPrice > 0)
                {
                    pricePerNight = configuredPrice;
                }
            }
            else
            {
                (roomName, imageUrl, pricePerNight) = GetFallbackRoom(roomId);
            }

            return new CheckoutViewModel
            {
                RoomId = roomId,
                RoomName = roomName,
                ImageUrl = imageUrl,
                PricePerNight = pricePerNight,
                CheckIn = checkIn,
                CheckOut = checkOut
            };
        }

        private static (string Name, string ImageUrl, decimal PricePerNight) GetFallbackRoom(string roomId)
        {
            return roomId switch
            {
                "2" => (
                    "Suite Cao Cấp",
                    "https://images.unsplash.com/photo-1582719478250-c89cae4dc85b?auto=format&fit=crop&w=800&q=80",
                    2_500_000m),
                "3" => (
                    "Venus Suite Cao Cấp",
                    "https://images.unsplash.com/photo-1631049307264-da0ec9d70304?q=80&w=800&auto=format&fit=crop",
                    3_200_000m),
                "4" => (
                    "Presidential Tổng Thống",
                    "https://images.unsplash.com/photo-1578683010236-d716f9a3f461?q=80&w=800&auto=format&fit=crop",
                    5_000_000m),
                _ => (
                    "Deluxe Hướng Biển",
                    "https://images.unsplash.com/photo-1611892440504-42a792e24d32?q=80&w=800&auto=format&fit=crop",
                    1_200_000m)
            };
        }

        private static string GetRoomImage(string roomName)
        {
            if (roomName.Contains("Standard", StringComparison.OrdinalIgnoreCase))
            {
                return "https://images.unsplash.com/photo-1611892440504-42a792e24d32?q=80&w=800&auto=format&fit=crop";
            }

            if (roomName.Contains("Deluxe", StringComparison.OrdinalIgnoreCase))
            {
                return "https://images.unsplash.com/photo-1582719478250-c89cae4dc85b?q=80&w=800&auto=format&fit=crop";
            }

            if (roomName.Contains("Suite", StringComparison.OrdinalIgnoreCase))
            {
                return "https://images.unsplash.com/photo-1631049307264-da0ec9d70304?q=80&w=800&auto=format&fit=crop";
            }

            return "https://images.unsplash.com/photo-1578683010236-d716f9a3f461?q=80&w=800&auto=format&fit=crop";
        }

        private async Task<bool> EnsureOnlineEmployeeAsync()
        {
            if (await _context.NhanViens.AnyAsync(nv => nv.MaNv == OnlineEmployeeId))
            {
                return true;
            }

            _context.NhanViens.Add(new NhanVien
            {
                MaNv = OnlineEmployeeId,
                HoTen = "Đặt phòng online",
                SoDienThoai = "0000000000",
                Email = "online@pbl3.local",
                ChucVu = "Hệ thống",
                TrangThai = DomainValues.NhanVienTrangThai.DangLam
            });

            try
            {
                await _context.SaveChangesAsync();
                return true;
            }
            catch (DbUpdateException)
            {
                _context.ChangeTracker.Clear();
                return await _context.NhanViens.AnyAsync(nv => nv.MaNv == OnlineEmployeeId);
            }
        }

    }
}
