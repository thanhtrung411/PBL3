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
            var bookingsData = await _context.DatPhongs
                .AsNoTracking()
                .OrderByDescending(x => x.NgayNhanPhong == today)
                .ThenByDescending(x => x.TrangThai == DomainValues.DatPhongTrangThai.DaNhanPhong)
                .ThenBy(x => x.NgayNhanPhong)
                .ThenByDescending(x => x.NgayDat)
                .Take(200)
                .Select(x => new
                {
                    x.MaDatPhong,
                    x.TenKhSnapshot,
                    x.SdtSnapshot,
                    x.CccdSnapshot,
                    x.NgayNhanPhong,
                    x.NgayTraPhong,
                    x.NgayDat,
                    x.TrangThai,
                    CustomerName = x.MaKhNavigation != null ? x.MaKhNavigation.HoTen : null,
                    CustomerPhone = x.MaKhNavigation != null ? x.MaKhNavigation.SoDienThoai : null,
                    CustomerEmail = x.MaKhNavigation != null ? x.MaKhNavigation.Email : null,
                    CustomerAddress = x.MaKhNavigation != null ? x.MaKhNavigation.DiaChi : null,
                    CustomerCccd = x.MaKhNavigation != null ? x.MaKhNavigation.Cccd : null,
                    InvoiceTongThanhToan = x.HoaDon != null ? (decimal?)x.HoaDon.TongThanhToan : null,
                    InvoiceSoTienDaThanhToan = x.HoaDon != null ? (decimal?)x.HoaDon.SoTienDaThanhToan : null,
                    InvoiceTrangThai = x.HoaDon != null ? x.HoaDon.TrangThai : null,
                    RoomLines = x.HoaDon != null
                        ? x.HoaDon.ChiTietHoaDons
                            .Where(ct => ct.LoaiMuc == DomainValues.ChiTietHoaDonLoaiMuc.Phong &&
                                         ct.TrangThai == DomainValues.ChiTietHoaDonTrangThai.HieuLuc)
                            .Select(ct => new
                            {
                                RoomNumber = ct.MaPhongNavigation != null ? ct.MaPhongNavigation.SoPhong : null,
                                RoomTypeName = ct.MaLoaiPhongNavigation != null
                                    ? ct.MaLoaiPhongNavigation.TenLoaiPhong
                                    : (ct.MaPhongNavigation != null && ct.MaPhongNavigation.MaLoaiPhongNavigation != null
                                        ? ct.MaPhongNavigation.MaLoaiPhongNavigation.TenLoaiPhong
                                        : null),
                                ct.SoNguoi,
                                ct.SoLuong,
                                ct.ThanhTien
                            })
                            .ToList()
                        : null
                })
                .ToListAsync();

            var viewModel = new AdminBookingManagementViewModel
            {
                Bookings = bookingsData.Select(x =>
                {
                    var roomNumbers = x.RoomLines?
                        .Where(r => r.RoomNumber != null)
                        .Select(r => r.RoomNumber!.Trim())
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .OrderBy(r => r)
                        .ToList() ?? new List<string>();
                    var roomTypes = x.RoomLines?
                        .Select(r => r.RoomTypeName)
                        .Where(r => !string.IsNullOrWhiteSpace(r))
                        .Select(r => r!.Trim())
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .OrderBy(r => r)
                        .ToList() ?? new List<string>();

                    var nights = Math.Max(1, x.NgayTraPhong.DayNumber - x.NgayNhanPhong.DayNumber);
                    var guests = Math.Max(1, x.RoomLines?.Sum(r => r.SoNguoi) ?? 0);
                    var total = x.InvoiceTongThanhToan ?? x.RoomLines?.Sum(r => r.ThanhTien) ?? 0;
                    var paid = x.InvoiceSoTienDaThanhToan ?? 0;
                    var (statusLabel, statusClass, statusIcon, filterStatus) = GetBookingStatus(x.TrangThai, x.NgayNhanPhong, x.NgayTraPhong, today);
                    var (paymentLabel, paymentClass) = GetPaymentStatus(x.InvoiceTrangThai, x.InvoiceSoTienDaThanhToan, x.InvoiceTongThanhToan);
                    var filterStatuses = GetFilterStatuses(x.TrangThai, x.NgayNhanPhong, x.NgayTraPhong, today, filterStatus);

                    // Build room summary
                    string roomSummary;
                    if (roomNumbers.Count > 0)
                    {
                        var typeLabel = roomTypes.Count > 0 ? $" · {string.Join(", ", roomTypes)}" : string.Empty;
                        roomSummary = $"Phòng {string.Join(", ", roomNumbers)}{typeLabel}";
                    }
                    else
                    {
                        var byRoomType = x.RoomLines?
                            .Where(r => !string.IsNullOrWhiteSpace(r.RoomTypeName))
                            .GroupBy(r => r.RoomTypeName!)
                            .Select(g => $"{g.Key} x{g.Sum(item => Math.Max(1, item.SoLuong))}")
                            .ToList() ?? new List<string>();
                        roomSummary = byRoomType.Count > 0 ? string.Join(", ", byRoomType) : "Chưa gán phòng";
                    }

                    return new AdminBookingManagementItemViewModel
                    {
                        BookingCode = x.MaDatPhong.Trim(),
                        GuestName = !string.IsNullOrWhiteSpace(x.TenKhSnapshot)
                            ? x.TenKhSnapshot.Trim()
                            : x.CustomerName?.Trim() ?? "Khách hàng",
                        Phone = FirstNonEmpty(x.SdtSnapshot, x.CustomerPhone),
                        Email = x.CustomerEmail,
                        Address = x.CustomerAddress,
                        IdentityNumber = FirstNonEmpty(x.CccdSnapshot, x.CustomerCccd),
                        RoomSummary = roomSummary,
                        RoomNumbers = roomNumbers.Count > 0 ? string.Join(", ", roomNumbers) : "Chưa gán phòng",
                        RoomTypes = roomTypes.Count > 0 ? string.Join(", ", roomTypes) : "Chưa có loại phòng",
                        CheckInLabel = FormatDate(x.NgayNhanPhong),
                        CheckOutLabel = FormatDate(x.NgayTraPhong),
                        DateRangeLabel = $"{FormatDate(x.NgayNhanPhong)} - {FormatDate(x.NgayTraPhong)}",
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
                        CreatedAtLabel = x.NgayDat.ToString("dd/MM/yyyy HH:mm", CultureInfo.GetCultureInfo("vi-VN"))
                    };
                }).ToList()
            };

            return View(viewModel);
        }

        private static (string Label, string ClassName, string Icon, string FilterStatus) GetBookingStatus(
            string trangThai,
            DateOnly ngayNhanPhong,
            DateOnly ngayTraPhong,
            DateOnly today)
        {
            if (trangThai == DomainValues.DatPhongTrangThai.DaHuy)
            {
                return ("Đã hủy", "completed", "bi-x-circle", "all");
            }

            if (trangThai == DomainValues.DatPhongTrangThai.QuaHanNhanPhong)
            {
                return ("Quá hạn nhận", "departure", "bi-exclamation-circle", "all");
            }

            if (trangThai == DomainValues.DatPhongTrangThai.TraPhong)
            {
                return ("Đã trả phòng", "completed", "bi-check2-circle", "all");
            }

            if (trangThai == DomainValues.DatPhongTrangThai.DaNhanPhong)
            {
                if (ngayTraPhong == today)
                {
                    return ("Trả hôm nay", "departure", "bi-box-arrow-right", "departure");
                }

                return ("Đang ở", "staying", "bi-house-check", "staying");
            }

            if (ngayNhanPhong == today)
            {
                return ("Đến hôm nay", "arrival", "bi-box-arrow-in-right", "arrival");
            }

            if (ngayNhanPhong > today)
            {
                return ("Sắp đến", "arrival", "bi-calendar-check", "all");
            }

            return ("Chờ xử lý", "departure", "bi-clock-history", "all");
        }

        private static List<string> GetFilterStatuses(
            string trangThai,
            DateOnly ngayNhanPhong,
            DateOnly ngayTraPhong,
            DateOnly today,
            string primaryFilter)
        {
            var result = new List<string>();
            var inactiveStatuses = new[]
            {
                DomainValues.DatPhongTrangThai.DaHuy,
                DomainValues.DatPhongTrangThai.TraPhong,
                DomainValues.DatPhongTrangThai.QuaHanNhanPhong
            };

            if (!inactiveStatuses.Contains(trangThai))
            {
                if (ngayNhanPhong == today)
                {
                    result.Add("arrival");
                }

                if (ngayNhanPhong <= today && ngayTraPhong >= today)
                {
                    result.Add("staying");
                }

                if (ngayTraPhong == today)
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

        private static (string Label, string ClassName) GetPaymentStatus(
            string? invoiceTrangThai,
            decimal? soTienDaThanhToan,
            decimal? tongThanhToan)
        {
            if (invoiceTrangThai == null)
            {
                return ("Chưa có hóa đơn", "unpaid");
            }

            if (invoiceTrangThai == DomainValues.HoaDonTrangThai.DaThanhToan ||
                soTienDaThanhToan >= tongThanhToan && tongThanhToan > 0)
            {
                return ("Đã trả", "paid");
            }

            if (soTienDaThanhToan > 0 ||
                invoiceTrangThai == DomainValues.HoaDonTrangThai.ThanhToanMotPhan)
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
