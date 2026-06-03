using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PBL3.Data;
using PBL3.Models;
using PBL3.Services.Interfaces;

namespace PBL3.Controllers
{
    public class CustomerController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IKhachHangService _khachHangService;

        public CustomerController(ApplicationDbContext context, IKhachHangService khachHangService)
        {
            _context = context;
            _khachHangService = khachHangService;
        }

        public async Task<IActionResult> Index()
        {
            var customers = await _context.KhachHangs
                .AsNoTracking()
                .OrderBy(x => x.HoTen)
                .ToListAsync();

            var bookingStats = await _context.DatPhongs
                .AsNoTracking()
                .Include(x => x.HoaDon)
                .Where(x => x.TrangThai != DomainValues.DatPhongTrangThai.DaHuy)
                .GroupBy(x => x.MaKh)
                .Select(x => new
                {
                    CustomerId = x.Key,
                    BookingCount = x.Count(),
                    TotalSpend = x.Sum(booking =>
                        booking.HoaDon != null && booking.HoaDon.TrangThai != DomainValues.HoaDonTrangThai.DaHuy
                            ? booking.HoaDon.TongThanhToan
                            : 0),
                    LastBookingAt = x.Max(booking => (DateTime?)booking.NgayDat)
                })
                .ToListAsync();
            var statsByCustomer = bookingStats.ToDictionary(x => x.CustomerId.Trim(), StringComparer.OrdinalIgnoreCase);

            var bookingHistory = await _context.DatPhongs
                .AsNoTracking()
                .AsSplitQuery()
                .Include(x => x.HoaDon)
                    .ThenInclude(x => x!.ChiTietHoaDons)
                        .ThenInclude(x => x.MaPhongNavigation)
                .Include(x => x.HoaDon)
                    .ThenInclude(x => x!.ChiTietHoaDons)
                        .ThenInclude(x => x.MaLoaiPhongNavigation)
                .OrderByDescending(x => x.NgayDat)
                .ToListAsync();
            var historyByCustomer = bookingHistory
                .GroupBy(x => x.MaKh.Trim(), StringComparer.OrdinalIgnoreCase)
                .ToDictionary(
                    x => x.Key,
                    x => x.Select(BuildCustomerBookingHistoryItem).ToList(),
                    StringComparer.OrdinalIgnoreCase);

            var viewModel = new AdminCustomerManagementViewModel
            {
                Customers = customers.Select(customer =>
                {
                    statsByCustomer.TryGetValue(customer.MaKh.Trim(), out var stats);
                    var totalSpend = stats?.TotalSpend ?? 0;
                    var tier = GetMemberTier(totalSpend);

                    return new AdminCustomerManagementItemViewModel
                    {
                        CustomerId = customer.MaKh.Trim(),
                        CustomerName = customer.HoTen.Trim(),
                        Gender = customer.GioiTinh?.Trim() ?? string.Empty,
                        IdentityNumber = customer.Cccd?.Trim() ?? string.Empty,
                        Phone = customer.SoDienThoai?.Trim() ?? string.Empty,
                        Email = customer.Email?.Trim() ?? string.Empty,
                        Address = customer.DiaChi?.Trim() ?? string.Empty,
                        Nationality = customer.QuocTich?.Trim() ?? string.Empty,
                        BirthDateLabel = customer.NgaySinh?.ToString("dd/MM/yyyy", CultureInfo.GetCultureInfo("vi-VN")) ?? "Chưa có",
                        BookingCount = stats?.BookingCount ?? 0,
                        TotalSpend = totalSpend,
                        TotalSpendLabel = FormatCurrency(totalSpend),
                        LastBookingLabel = stats?.LastBookingAt?.ToString("dd/MM/yyyy", CultureInfo.GetCultureInfo("vi-VN")) ?? "Chưa đặt",
                        LastBookingSortValue = stats?.LastBookingAt?.Ticks ?? 0,
                        MemberTier = tier,
                        MemberTierRank = GetMemberTierRank(tier),
                        MemberTierClass = GetMemberTierClass(tier),
                        MemberTierStyle = GetMemberTierStyle(tier),
                        Initials = GetInitials(customer.HoTen),
                        BookingHistory = historyByCustomer.TryGetValue(customer.MaKh.Trim(), out var history)
                            ? history
                            : new List<AdminCustomerBookingHistoryItemViewModel>()
                    };
                }).ToList()
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(AdminCustomerUpdateRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.MaKh))
            {
                TempData["CustomerError"] = "Thiếu mã khách hàng.";
                return RedirectToAction(nameof(Index));
            }

            if (string.IsNullOrWhiteSpace(request.HoTen))
            {
                TempData["CustomerError"] = "Vui lòng nhập họ tên khách hàng.";
                return RedirectToAction(nameof(Index));
            }

            var customer = await _khachHangService.GetByIdAsync(request.MaKh.Trim());
            if (customer == null)
            {
                TempData["CustomerError"] = "Không tìm thấy khách hàng cần cập nhật.";
                return RedirectToAction(nameof(Index));
            }

            customer.HoTen = request.HoTen;
            customer.GioiTinh = request.GioiTinh;
            customer.Cccd = request.Cccd;
            customer.SoDienThoai = request.SoDienThoai;
            customer.Email = request.Email;
            customer.DiaChi = request.DiaChi;
            customer.QuocTich = request.QuocTich;

            var updated = await _khachHangService.UpdateAsync(customer);
            TempData[updated ? "CustomerSuccess" : "CustomerError"] = updated
                ? "Đã cập nhật thông tin khách hàng."
                : "Không thể cập nhật khách hàng. CCCD có thể đã tồn tại.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                TempData["CustomerError"] = "Thiếu mã khách hàng.";
                return RedirectToAction(nameof(Index));
            }

            var deleted = await _khachHangService.DeleteAsync(id.Trim());
            TempData[deleted ? "CustomerSuccess" : "CustomerError"] = deleted
                ? "Đã xóa khách hàng."
                : "Không thể xóa khách hàng vì đã phát sinh dữ liệu đặt phòng.";
            return RedirectToAction(nameof(Index));
        }

        private static string GetMemberTier(decimal totalSpend)
        {
            if (totalSpend >= 30_000_000)
            {
                return "Bạch Kim";
            }

            if (totalSpend >= 15_000_000)
            {
                return "Vàng";
            }

            if (totalSpend >= 5_000_000)
            {
                return "Bạc";
            }

            return "Thường";
        }

        private static string GetMemberTierClass(string tier)
        {
            return tier switch
            {
                "Vàng" => "bg-warning bg-opacity-10 text-warning border border-warning",
                "Bạc" => "bg-secondary bg-opacity-10 text-secondary border border-secondary",
                "Bạch Kim" => "border",
                _ => "bg-light text-muted border"
            };
        }

        private static int GetMemberTierRank(string tier)
        {
            return tier switch
            {
                "Bạch Kim" => 4,
                "Vàng" => 3,
                "Bạc" => 2,
                _ => 1
            };
        }

        private static AdminCustomerBookingHistoryItemViewModel BuildCustomerBookingHistoryItem(DatPhong booking)
        {
            var roomCodes = GetRoomCodes(booking).ToList();
            var roomLabel = roomCodes.Count > 0 ? string.Join(", ", roomCodes) : "Chưa gán phòng";
            var total = booking.HoaDon?.TongThanhToan ?? 0;
            var bookingDate = booking.NgayDat.ToString("dd/MM/yyyy", CultureInfo.GetCultureInfo("vi-VN"));
            var checkIn = booking.NgayNhanPhong.ToString("dd/MM/yyyy", CultureInfo.GetCultureInfo("vi-VN"));
            var checkOut = booking.NgayTraPhong.ToString("dd/MM/yyyy", CultureInfo.GetCultureInfo("vi-VN"));
            var status = GetBookingStatusLabel(booking.TrangThai);

            return new AdminCustomerBookingHistoryItemViewModel
            {
                BookingCode = booking.MaDatPhong.Trim(),
                BookingDateLabel = bookingDate,
                CheckInLabel = checkIn,
                CheckOutLabel = checkOut,
                RoomCodes = roomLabel,
                TotalLabel = FormatCurrency(total),
                StatusLabel = status,
                SearchText = $"{booking.MaDatPhong} {bookingDate} {checkIn} {checkOut} {roomLabel} {status}".ToLowerInvariant()
            };
        }

        private static IEnumerable<string> GetRoomCodes(DatPhong booking)
        {
            var roomLines = booking.HoaDon?.ChiTietHoaDons
                .Where(x => x.LoaiMuc == DomainValues.ChiTietHoaDonLoaiMuc.Phong &&
                            x.TrangThai == DomainValues.ChiTietHoaDonTrangThai.HieuLuc) ??
                Enumerable.Empty<ChiTietHoaDon>();

            var roomCodes = roomLines
                .Where(x => x.MaPhongNavigation != null)
                .Select(x => x.MaPhongNavigation!.SoPhong.Trim())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(x => x)
                .ToList();

            if (roomCodes.Count > 0)
            {
                return roomCodes;
            }

            return roomLines
                .Select(x => x.MaLoaiPhongNavigation?.TenLoaiPhong?.Trim())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x!)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(x => x);
        }

        private static string GetBookingStatusLabel(string? status)
        {
            return status switch
            {
                DomainValues.DatPhongTrangThai.GiuCho => "Giữ chỗ",
                DomainValues.DatPhongTrangThai.DaDatCoc => "Đã đặt cọc",
                DomainValues.DatPhongTrangThai.DaNhanPhong => "Đã nhận phòng",
                DomainValues.DatPhongTrangThai.TraPhong => "Đã trả phòng",
                DomainValues.DatPhongTrangThai.DaHuy => "Đã hủy",
                DomainValues.DatPhongTrangThai.QuaHanNhanPhong => "Quá hạn nhận phòng",
                _ => string.IsNullOrWhiteSpace(status) ? "Chưa rõ" : status.Trim()
            };
        }

        private static string GetMemberTierStyle(string tier)
        {
            return tier == "Bạch Kim"
                ? "background-color: #f3e8ff; color: #a855f7; border-color: #d8b4fe !important;"
                : string.Empty;
        }

        private static string GetInitials(string fullName)
        {
            var words = fullName
                .Trim()
                .Split(' ', StringSplitOptions.RemoveEmptyEntries);

            if (words.Length >= 2)
            {
                return $"{words[^2][0]}{words[^1][0]}".ToUpperInvariant();
            }

            return words.Length == 1
                ? words[0][..Math.Min(2, words[0].Length)].ToUpperInvariant()
                : "KH";
        }

        private static string FormatCurrency(decimal value)
        {
            return $"{value.ToString("N0", CultureInfo.GetCultureInfo("vi-VN"))} đ";
        }
    }
}
