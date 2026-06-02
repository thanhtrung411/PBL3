using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PBL3.Data;
using PBL3.Models;

namespace PBL3.Controllers
{
    public class BookingManagementController : Controller
    {
        private readonly ApplicationDbContext _context;

        public BookingManagementController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var today = DateOnly.FromDateTime(DateTime.Today);
            var bookings = await _context.DatPhongs
                .AsNoTracking()
                .Include(x => x.MaKhNavigation)
                .Include(x => x.HoaDon)
                    .ThenInclude(x => x!.ChiTietHoaDons)
                        .ThenInclude(x => x.MaPhongNavigation)
                .Include(x => x.HoaDon)
                    .ThenInclude(x => x!.ChiTietHoaDons)
                        .ThenInclude(x => x.MaLoaiPhongNavigation)
                .OrderByDescending(x => x.NgayNhanPhong == today)
                .ThenByDescending(x => x.TrangThai == DomainValues.DatPhongTrangThai.DaNhanPhong)
                .ThenBy(x => x.NgayNhanPhong)
                .ThenByDescending(x => x.NgayDat)
                .Take(200)
                .ToListAsync();

            var viewModel = new AdminBookingManagementViewModel
            {
                Bookings = bookings.Select(x => BuildBookingItem(x, today)).ToList()
            };

            return View(viewModel);
        }

        private static AdminBookingManagementItemViewModel BuildBookingItem(DatPhong booking, DateOnly today)
        {
            var roomLines = GetRoomLines(booking).ToList();
            var roomNumbers = roomLines
                .Where(x => x.MaPhongNavigation != null)
                .Select(x => x.MaPhongNavigation!.SoPhong.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(x => x)
                .ToList();
            var roomTypes = roomLines
                .Select(x => x.MaLoaiPhongNavigation?.TenLoaiPhong ?? x.MaPhongNavigation?.MaLoaiPhongNavigation?.TenLoaiPhong)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x!.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(x => x)
                .ToList();

            var nights = Math.Max(1, booking.NgayTraPhong.DayNumber - booking.NgayNhanPhong.DayNumber);
            var guests = Math.Max(1, roomLines.Sum(x => x.SoNguoi));
            var invoice = booking.HoaDon;
            var total = invoice?.TongThanhToan ?? roomLines.Sum(x => x.ThanhTien);
            var paid = invoice?.SoTienDaThanhToan ?? 0;
            var (statusLabel, statusClass, statusIcon, filterStatus) = GetBookingStatus(booking, today);
            var (paymentLabel, paymentClass) = GetPaymentStatus(invoice);
            var filterStatuses = GetFilterStatuses(booking, today, filterStatus);
            var customer = booking.MaKhNavigation;

            return new AdminBookingManagementItemViewModel
            {
                BookingCode = booking.MaDatPhong.Trim(),
                GuestName = !string.IsNullOrWhiteSpace(booking.TenKhSnapshot)
                    ? booking.TenKhSnapshot.Trim()
                    : customer?.HoTen.Trim() ?? "Khách hàng",
                Phone = FirstNonEmpty(booking.SdtSnapshot, customer?.SoDienThoai),
                Email = customer?.Email,
                Address = customer?.DiaChi,
                IdentityNumber = FirstNonEmpty(booking.CccdSnapshot, customer?.Cccd),
                RoomSummary = BuildRoomSummary(roomLines, roomNumbers, roomTypes),
                RoomNumbers = roomNumbers.Count > 0 ? string.Join(", ", roomNumbers) : "Chưa gán phòng",
                RoomTypes = roomTypes.Count > 0 ? string.Join(", ", roomTypes) : "Chưa có loại phòng",
                CheckInLabel = FormatDate(booking.NgayNhanPhong),
                CheckOutLabel = FormatDate(booking.NgayTraPhong),
                DateRangeLabel = $"{FormatDate(booking.NgayNhanPhong)} - {FormatDate(booking.NgayTraPhong)}",
                NightsLabel = $"{nights} đêm",
                GuestsLabel = $"{guests} khách",
                TotalLabel = FormatCurrency(total),
                PaidLabel = FormatCurrency(paid),
                PaymentStatusLabel = paymentLabel,
                PaymentStatusClass = paymentClass,
                BookingStatusLabel = statusLabel,
                BookingStatusClass = statusClass,
                BookingStatusIcon = statusIcon,
                FilterStatus = filterStatus,
                FilterStatuses = string.Join(" ", filterStatuses),
                CreatedAtLabel = booking.NgayDat.ToString("dd/MM/yyyy HH:mm", CultureInfo.GetCultureInfo("vi-VN"))
            };
        }

        private static IEnumerable<ChiTietHoaDon> GetRoomLines(DatPhong booking)
        {
            return booking.HoaDon?.ChiTietHoaDons
                .Where(x => x.LoaiMuc == DomainValues.ChiTietHoaDonLoaiMuc.Phong &&
                            x.TrangThai == DomainValues.ChiTietHoaDonTrangThai.HieuLuc) ??
                   Enumerable.Empty<ChiTietHoaDon>();
        }

        private static string BuildRoomSummary(
            IReadOnlyCollection<ChiTietHoaDon> roomLines,
            IReadOnlyCollection<string> roomNumbers,
            IReadOnlyCollection<string> roomTypes)
        {
            if (roomNumbers.Count > 0)
            {
                var typeLabel = roomTypes.Count > 0 ? $" · {string.Join(", ", roomTypes)}" : string.Empty;
                return $"Phòng {string.Join(", ", roomNumbers)}{typeLabel}";
            }

            var byRoomType = roomLines
                .Where(x => x.MaLoaiPhongNavigation != null)
                .GroupBy(x => x.MaLoaiPhongNavigation!.TenLoaiPhong)
                .Select(x => $"{x.Key} x{x.Sum(item => Math.Max(1, item.SoLuong))}")
                .ToList();

            return byRoomType.Count > 0 ? string.Join(", ", byRoomType) : "Chưa gán phòng";
        }

        private static (string Label, string ClassName, string Icon, string FilterStatus) GetBookingStatus(
            DatPhong booking,
            DateOnly today)
        {
            if (booking.TrangThai == DomainValues.DatPhongTrangThai.DaHuy)
            {
                return ("Đã hủy", "completed", "bi-x-circle", "all");
            }

            if (booking.TrangThai == DomainValues.DatPhongTrangThai.QuaHanNhanPhong)
            {
                return ("Quá hạn nhận", "departure", "bi-exclamation-circle", "all");
            }

            if (booking.TrangThai == DomainValues.DatPhongTrangThai.TraPhong)
            {
                return ("Đã trả phòng", "completed", "bi-check2-circle", "all");
            }

            if (booking.TrangThai == DomainValues.DatPhongTrangThai.DaNhanPhong)
            {
                if (booking.NgayTraPhong == today)
                {
                    return ("Trả hôm nay", "departure", "bi-box-arrow-right", "departure");
                }

                return ("Đang ở", "staying", "bi-house-check", "staying");
            }

            if (booking.NgayNhanPhong == today)
            {
                return ("Đến hôm nay", "arrival", "bi-box-arrow-in-right", "arrival");
            }

            if (booking.NgayNhanPhong > today)
            {
                return ("Sắp đến", "arrival", "bi-calendar-check", "all");
            }

            return ("Chờ xử lý", "departure", "bi-clock-history", "all");
        }

        private static List<string> GetFilterStatuses(DatPhong booking, DateOnly today, string primaryFilter)
        {
            var result = new List<string>();
            var inactiveStatuses = new[]
            {
                DomainValues.DatPhongTrangThai.DaHuy,
                DomainValues.DatPhongTrangThai.TraPhong,
                DomainValues.DatPhongTrangThai.QuaHanNhanPhong
            };

            if (!inactiveStatuses.Contains(booking.TrangThai))
            {
                if (booking.NgayNhanPhong == today)
                {
                    result.Add("arrival");
                }

                if (booking.NgayNhanPhong <= today && booking.NgayTraPhong >= today)
                {
                    result.Add("staying");
                }

                if (booking.NgayTraPhong == today)
                {
                    result.Add("departure");
                }
            }

            if (primaryFilter != "all")
            {
                result.Add(primaryFilter);
            }

            result.Add("all");
            return result.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        }

        private static (string Label, string ClassName) GetPaymentStatus(HoaDon? invoice)
        {
            if (invoice == null)
            {
                return ("Chưa có hóa đơn", "unpaid");
            }

            if (invoice.TrangThai == DomainValues.HoaDonTrangThai.DaThanhToan ||
                invoice.SoTienDaThanhToan >= invoice.TongThanhToan && invoice.TongThanhToan > 0)
            {
                return ("Đã trả", "paid");
            }

            if (invoice.SoTienDaThanhToan > 0 ||
                invoice.TrangThai == DomainValues.HoaDonTrangThai.ThanhToanMotPhan)
            {
                return ("Trả một phần", "unpaid");
            }

            return ("Chưa thanh toán", "unpaid");
        }

        private static string? FirstNonEmpty(params string?[] values)
        {
            return values.FirstOrDefault(x => !string.IsNullOrWhiteSpace(x))?.Trim();
        }

        private static string FormatDate(DateOnly date)
        {
            return date.ToString("dd/MM/yyyy", CultureInfo.GetCultureInfo("vi-VN"));
        }

        private static string FormatCurrency(decimal value)
        {
            return $"{value.ToString("N0", CultureInfo.GetCultureInfo("vi-VN"))} đ";
        }
    }
}
