using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using PBL3.Data;
using PBL3.Models;
using PBL3.Services.Interfaces;
using System.Data.Common;

namespace PBL3.Services;

public class PublicBookingService : IPublicBookingService
{
    private const string OnlineEmployeeId = "NV_ONLINE";

    private readonly ApplicationDbContext _context;
    private readonly IDatPhongService _datPhongService;
    private readonly IKhachHangService _khachHangService;
    private readonly IBangGiaPhongService _bangGiaPhongService;
    private readonly IChiTietHoaDonService _chiTietHoaDonService;
    private readonly IHoaDonService _hoaDonService;
    private readonly VnPayOptions _vnPayOptions;

    public PublicBookingService(
        ApplicationDbContext context,
        IDatPhongService datPhongService,
        IKhachHangService khachHangService,
        IBangGiaPhongService bangGiaPhongService,
        IChiTietHoaDonService chiTietHoaDonService,
        IHoaDonService hoaDonService,
        IOptions<VnPayOptions> vnPayOptions)
    {
        _context = context;
        _datPhongService = datPhongService;
        _khachHangService = khachHangService;
        _bangGiaPhongService = bangGiaPhongService;
        _chiTietHoaDonService = chiTietHoaDonService;
        _hoaDonService = hoaDonService;
        _vnPayOptions = vnPayOptions.Value;
    }

    public async Task<RoomSearchViewModel> SearchRoomsAsync(DateTime? checkIn, DateTime? checkOut, int? guests, string? roomTypeId)
    {
        var (normalizedCheckIn, normalizedCheckOut) = NormalizeDates(checkIn, checkOut);
        var normalizedGuests = Math.Clamp(guests ?? 2, 1, 12);
        var selectedRoomType = string.IsNullOrWhiteSpace(roomTypeId) ? null : NormalizeCode(roomTypeId);
        var model = new RoomSearchViewModel
        {
            CheckIn = normalizedCheckIn,
            CheckOut = normalizedCheckOut,
            Guests = normalizedGuests,
            RoomTypeId = selectedRoomType
        };

        try
        {
            var roomTypes = await _context.LoaiPhongs
                .AsNoTracking()
                .Where(x => x.SoNguoiToiDa >= normalizedGuests)
                .OrderBy(x => x.MaLoaiPhong)
                .ToListAsync();

            if (!string.IsNullOrWhiteSpace(selectedRoomType))
            {
                roomTypes = roomTypes
                    .Where(x => NormalizeCode(x.MaLoaiPhong) == selectedRoomType)
                    .ToList();
            }

            var results = new List<PublicRoomOptionViewModel>();
            var from = DateOnly.FromDateTime(normalizedCheckIn);
            var to = DateOnly.FromDateTime(normalizedCheckOut);

            foreach (var roomType in roomTypes)
            {
                var normalizedRoomTypeId = NormalizeCode(roomType.MaLoaiPhong);
                var price = await _bangGiaPhongService.LayGiaPhongHienTaiAsync(normalizedRoomTypeId);
                if (price <= 0)
                {
                    price = GetFallbackPrice(roomType.TenLoaiPhong);
                }

                var availableRooms = await CountAvailableRoomsAsync(normalizedRoomTypeId, from, to);

                results.Add(new PublicRoomOptionViewModel
                {
                    RoomTypeId = normalizedRoomTypeId,
                    RoomName = roomType.TenLoaiPhong,
                    Description = string.IsNullOrWhiteSpace(roomType.MoTa)
                        ? "Phòng chất lượng cao với đầy đủ tiện nghi cơ bản cho kỳ nghỉ thoải mái."
                        : roomType.MoTa,
                    MaxGuests = roomType.SoNguoiToiDa,
                    PricePerNight = price,
                    ImageUrl = GetRoomImage(roomType.TenLoaiPhong),
                    AvailableRooms = availableRooms
                });
            }

            model.Results = results;
            return model;
        }
        catch (Exception ex) when (IsDatabaseException(ex))
        {
            model.ErrorMessage = "Không thể kết nối cơ sở dữ liệu để tải danh sách phòng. Vui lòng thử lại sau.";
            return model;
        }
    }

    public async Task<CheckoutViewModel?> BuildCheckoutAsync(string roomTypeId, DateTime? checkIn, DateTime? checkOut, int? guests)
    {
        if (string.IsNullOrWhiteSpace(roomTypeId))
        {
            return null;
        }

        var normalizedRoomTypeId = NormalizeCode(roomTypeId);
        var search = await SearchRoomsAsync(checkIn, checkOut, guests, normalizedRoomTypeId);
        var room = search.Results.FirstOrDefault(x => NormalizeCode(x.RoomTypeId) == normalizedRoomTypeId);
        if (room == null)
        {
            return null;
        }

        var vnPayAvailable = IsVnPayConfigured();

        return new CheckoutViewModel
        {
            RoomId = room.RoomTypeId,
            RoomName = room.RoomName,
            ImageUrl = room.ImageUrl,
            PricePerNight = room.PricePerNight,
            CheckIn = search.CheckIn,
            CheckOut = search.CheckOut,
            Guests = search.Guests,
            PaymentMethod = vnPayAvailable ? PaymentMethods.VnPay : PaymentMethods.PayAtHotel,
            VnPayAvailable = vnPayAvailable,
            PaymentUnavailableMessage = vnPayAvailable ? null : "VNPay chÆ°a Ä‘Æ°á»£c cáº¥u hÃ¬nh merchant. Báº¡n váº«n cÃ³ thá»ƒ giá»¯ chá»— vÃ  thanh toÃ¡n táº¡i khÃ¡ch sáº¡n.",
            TotalAmount = room.PricePerNight * Math.Max((search.CheckOut - search.CheckIn).Days, 1)
        };
    }

    public async Task<PublicBookingResult> ConfirmBookingAsync(CheckoutViewModel model)
    {
        var (checkIn, checkOut) = NormalizeDates(model.CheckIn, model.CheckOut);
        var checkInDate = DateOnly.FromDateTime(checkIn);
        var checkOutDate = DateOnly.FromDateTime(checkOut);
        var guests = Math.Clamp(model.Guests <= 0 ? 2 : model.Guests, 1, 12);
        var roomTypeId = NormalizeCode(model.RoomId);
        if (string.IsNullOrWhiteSpace(roomTypeId))
        {
            return Fail("Vui lòng chọn loại phòng trước khi xác nhận đặt phòng.");
        }

        var roomType = await _context.LoaiPhongs
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.MaLoaiPhong == roomTypeId);
        if (roomType == null)
        {
            return Fail("Loại phòng không tồn tại. Vui lòng chọn lại phòng.");
        }

        if (guests > roomType.SoNguoiToiDa)
        {
            return Fail("Số khách vượt quá sức chứa của loại phòng đã chọn.");
        }

        var price = await _bangGiaPhongService.LayGiaPhongHienTaiAsync(roomTypeId);
        if (price <= 0)
        {
            price = GetFallbackPrice(roomType.TenLoaiPhong);
        }

        var availableRooms = await CountAvailableRoomsAsync(roomTypeId, checkInDate, checkOutDate);
        if (availableRooms <= 0)
        {
            return Fail("Loại phòng này vừa hết chỗ trong khoảng ngày bạn chọn. Vui lòng chọn ngày hoặc phòng khác.");
        }

        var executionStrategy = _context.Database.CreateExecutionStrategy();
        return await executionStrategy.ExecuteAsync(async () =>
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();

            if (!await EnsureOnlineEmployeeAsync())
            {
                return Fail("Không thể chuẩn bị nhân viên xử lý đặt phòng online. Vui lòng thử lại.");
            }

            var customer = await UpsertCustomerAsync(model);
            if (customer == null)
            {
                return Fail("Không thể tạo hoặc cập nhật thông tin khách hàng. Vui lòng thử lại.");
            }

            var bookingCode = await CodeGenerator.GenerateFromSequenceAsync(_context, "dbo.Seq_DatPhong", "DP", 8);
            if (bookingCode == null)
            {
                return Fail("Không thể tạo mã đặt phòng. Vui lòng thử lại.");
            }

            var booking = new DatPhong
            {
                MaDatPhong = bookingCode,
                MaKh = customer.MaKh,
                MaNv = OnlineEmployeeId,
                TenKhSnapshot = model.CustomerName.Trim(),
                CccdSnapshot = model.Cccd.Trim(),
                SdtSnapshot = model.PhoneNumber.Trim(),
                NgayDat = DateTime.Now,
                NgayNhanPhong = checkInDate,
                NgayTraPhong = checkOutDate,
                TrangThai = DomainValues.DatPhongTrangThai.GiuCho,
                GhiChu = model.Note
            };

            if (!await _datPhongService.CreateAsync(booking))
            {
                return Fail("Lỗi hệ thống khi tạo phiếu đặt phòng. Vui lòng thử lại.");
            }

            var invoice = await _context.HoaDons.FirstOrDefaultAsync(x => x.MaDatPhong == bookingCode);
            if (invoice == null)
            {
                return Fail("Không tìm thấy hóa đơn vừa tạo. Vui lòng liên hệ lễ tân để kiểm tra lại đặt phòng.");
            }

            var nights = Math.Max(checkOutDate.DayNumber - checkInDate.DayNumber, 1);
            var roomTotal = price * nights;
            invoice.TongTienPhong = roomTotal;
            invoice.TongTienDichVu = 0;
            invoice.TienDatCoc = 0;
            invoice.TienGiamGiaPhong = 0;
            invoice.TongThanhToan = roomTotal;
            invoice.SoTienDaThanhToan = 0;
            invoice.PhuongThucThanhToan = model.PaymentMethod == PaymentMethods.PayAtHotel
                ? DomainValues.PhuongThucThanhToan.TienMat
                : DomainValues.PhuongThucThanhToan.Qr;
            invoice.TrangThai = DomainValues.HoaDonTrangThai.ChuaThanhToan;

            if (!await _hoaDonService.UpdateAsync(invoice))
            {
                return Fail("Không thể cập nhật hóa đơn. Vui lòng liên hệ lễ tân để kiểm tra lại đặt phòng.");
            }

            var detailCode = await CodeGenerator.GenerateFromSequenceAsync(_context, "dbo.Seq_ChiTietHoaDon", "CT", 8);
            if (detailCode == null)
            {
                return Fail("Không thể tạo mã chi tiết hóa đơn. Vui lòng liên hệ lễ tân để kiểm tra lại đặt phòng.");
            }

            var detail = new ChiTietHoaDon
            {
                MaCthd = detailCode,
                MaHoaDon = invoice.MaHoaDon,
                LoaiMuc = DomainValues.ChiTietHoaDonLoaiMuc.Phong,
                NoiDung = $"Thuê phòng loại {roomTypeId} - {roomType.TenLoaiPhong}",
                NgayApDung = checkInDate,
                SoNguoi = guests,
                SoLuong = nights,
                DonGia = price,
                ThanhTien = roomTotal,
                TrangThai = DomainValues.ChiTietHoaDonTrangThai.HieuLuc
            };

            if (!await _chiTietHoaDonService.CreateAsync(detail))
            {
                return Fail("Không thể tạo chi tiết hóa đơn. Vui lòng liên hệ lễ tân để kiểm tra lại đặt phòng.");
            }

            await transaction.CommitAsync();
            return new PublicBookingResult { Success = true, BookingCode = bookingCode };
        });
    }

    public async Task<BookingLookupResultViewModel?> LookupAsync(string bookingCode, string phoneNumber)
    {
        var normalizedCode = bookingCode.Trim();
        var normalizedPhone = phoneNumber.Trim();

        var booking = await _context.DatPhongs
            .AsNoTracking()
            .Include(x => x.HoaDon)
            .ThenInclude(x => x!.ChiTietHoaDons)
            .FirstOrDefaultAsync(x => x.MaDatPhong == normalizedCode && x.SdtSnapshot == normalizedPhone);

        if (booking == null)
        {
            return null;
        }

        var roomDetail = booking.HoaDon?.ChiTietHoaDons
            .FirstOrDefault(x => x.LoaiMuc == DomainValues.ChiTietHoaDonLoaiMuc.Phong);

        return new BookingLookupResultViewModel
        {
            BookingCode = booking.MaDatPhong,
            CustomerName = booking.TenKhSnapshot,
            PhoneNumber = booking.SdtSnapshot ?? "",
            CheckIn = booking.NgayNhanPhong,
            CheckOut = booking.NgayTraPhong,
            Status = booking.TrangThai,
            RoomSummary = roomDetail?.NoiDung ?? "Thông tin phòng đang được cập nhật",
            TotalAmount = booking.HoaDon?.TongThanhToan ?? 0
        };
    }

    private async Task<KhachHang?> UpsertCustomerAsync(CheckoutViewModel model)
    {
        var cccd = model.Cccd.Trim();
        var customer = await _khachHangService.GetByCccdAsync(cccd);

        if (customer == null)
        {
            var customerCode = await CodeGenerator.GenerateFromSequenceAsync(_context, "dbo.Seq_KhachHang", "KH", 8);
            if (customerCode == null)
            {
                return null;
            }

            customer = new KhachHang
            {
                MaKh = customerCode,
                Cccd = cccd,
                HoTen = model.CustomerName.Trim(),
                SoDienThoai = model.PhoneNumber.Trim(),
                Email = model.Email.Trim()
            };

            return await _khachHangService.CreateAsync(customer) ? customer : null;
        }

        customer.HoTen = model.CustomerName.Trim();
        customer.SoDienThoai = model.PhoneNumber.Trim();
        customer.Email = model.Email.Trim();
        return await _khachHangService.UpdateAsync(customer) ? customer : null;
    }

    private async Task<int> CountAvailableRoomsAsync(string roomTypeId, DateOnly checkIn, DateOnly checkOut)
    {
        var normalizedRoomTypeId = NormalizeCode(roomTypeId);
        var totalRooms = await _context.Phongs
            .AsNoTracking()
            .Where(x => x.MaLoaiPhong == normalizedRoomTypeId &&
                        x.TrangThai != "Bảo trì" &&
                        x.TrangThai != "Ngưng sử dụng")
            .CountAsync();

        if (totalRooms <= 0)
        {
            return 1;
        }

        var overlappingBookings = await _context.ChiTietHoaDons
            .AsNoTracking()
            .Include(x => x.MaPhongNavigation)
            .Include(x => x.MaHoaDonNavigation)
            .ThenInclude(x => x.MaDatPhongNavigation)
            .Where(x => x.LoaiMuc == DomainValues.ChiTietHoaDonLoaiMuc.Phong &&
                        x.MaHoaDonNavigation.MaDatPhongNavigation.TrangThai != DomainValues.DatPhongTrangThai.DaHuy &&
                        x.MaHoaDonNavigation.MaDatPhongNavigation.TrangThai != DomainValues.DatPhongTrangThai.TraPhong &&
                        x.MaHoaDonNavigation.MaDatPhongNavigation.NgayNhanPhong < checkOut &&
                        x.MaHoaDonNavigation.MaDatPhongNavigation.NgayTraPhong > checkIn &&
                        ((x.MaPhong != null && x.MaPhongNavigation!.MaLoaiPhong == normalizedRoomTypeId) ||
                         (x.MaPhong == null && x.NoiDung.Contains(normalizedRoomTypeId))))
            .CountAsync();

        return Math.Max(totalRooms - overlappingBookings, 0);
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

    private static (DateTime CheckIn, DateTime CheckOut) NormalizeDates(DateTime? checkIn, DateTime? checkOut)
    {
        var normalizedCheckIn = (checkIn ?? DateTime.Today).Date;
        var normalizedCheckOut = (checkOut ?? normalizedCheckIn.AddDays(1)).Date;
        if (normalizedCheckOut <= normalizedCheckIn)
        {
            normalizedCheckOut = normalizedCheckIn.AddDays(1);
        }

        return (normalizedCheckIn, normalizedCheckOut);
    }

    private static PublicBookingResult Fail(string message)
    {
        return new PublicBookingResult { Success = false, ErrorMessage = message };
    }

    private static bool IsDatabaseException(Exception ex)
    {
        return ex is DbException ||
               ex is TimeoutException ||
               ex is InvalidOperationException { InnerException: DbException } ||
               ex.InnerException is DbException;
    }

    private bool IsVnPayConfigured()
    {
        return _vnPayOptions.Enabled &&
               !string.IsNullOrWhiteSpace(_vnPayOptions.PaymentUrl) &&
               !string.IsNullOrWhiteSpace(_vnPayOptions.TmnCode) &&
               !string.IsNullOrWhiteSpace(_vnPayOptions.HashSecret);
    }

    private static string GetRoomImage(string roomName)
    {
        if (roomName.Contains("Standard", StringComparison.OrdinalIgnoreCase))
        {
            return "/images/standard-room.jpg";
        }

        if (roomName.Contains("Deluxe", StringComparison.OrdinalIgnoreCase))
        {
            return "/images/deluxe-room.jpg";
        }

        if (roomName.Contains("Suite", StringComparison.OrdinalIgnoreCase))
        {
            return "/images/suite-room.jpg";
        }

        if (roomName.Contains("President", StringComparison.OrdinalIgnoreCase))
        {
            return "/images/president-room.jpg";
        }

        return "/images/deluxe-room.jpg";
    }

    private static string NormalizeCode(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
    }

    private static decimal GetFallbackPrice(string roomName)
    {
        if (roomName.Contains("President", StringComparison.OrdinalIgnoreCase))
        {
            return 2_500_000m;
        }

        if (roomName.Contains("Suite", StringComparison.OrdinalIgnoreCase))
        {
            return 1_200_000m;
        }

        if (roomName.Contains("Deluxe", StringComparison.OrdinalIgnoreCase))
        {
            return 800_000m;
        }

        return 500_000m;
    }
}
