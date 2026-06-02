using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PBL3.Data;
using PBL3.Models;

namespace PBL3.Controllers
{
    public class InvoiceController : Controller
    {
        private readonly ApplicationDbContext _context;

        public InvoiceController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var today = DateOnly.FromDateTime(DateTime.Today);
            var invoices = await _context.HoaDons
                .AsNoTracking()
                .Include(x => x.MaDatPhongNavigation)
                    .ThenInclude(x => x.MaKhNavigation)
                .Include(x => x.MaDatPhongNavigation)
                    .ThenInclude(x => x.MaNvNavigation)
                .Include(x => x.ChiTietHoaDons)
                    .ThenInclude(x => x.MaPhongNavigation)
                .Include(x => x.ChiTietHoaDons)
                    .ThenInclude(x => x.MaLoaiPhongNavigation)
                .Include(x => x.ChiTietHoaDons)
                    .ThenInclude(x => x.MaDvNavigation)
                .OrderByDescending(x => x.NgayThanhToanCuoi ?? x.MaDatPhongNavigation.NgayDat)
                .Take(300)
                .ToListAsync();

            var items = invoices.Select(invoice => BuildInvoiceItem(invoice, today)).ToList();
            var viewModel = new AdminInvoiceManagementViewModel
            {
                Invoices = items,
                Employees = items
                    .Where(x => !string.IsNullOrWhiteSpace(x.EmployeeId))
                    .GroupBy(x => x.EmployeeId, StringComparer.OrdinalIgnoreCase)
                    .Select(x => new AdminInvoiceEmployeeFilterOptionViewModel
                    {
                        EmployeeId = x.Key,
                        EmployeeName = x.First().EmployeeName
                    })
                    .OrderBy(x => x.EmployeeName)
                    .ToList(),
                TotalPaid = invoices.Sum(x => x.SoTienDaThanhToan),
                TotalRemaining = invoices.Sum(x => Math.Max(x.TongThanhToan - x.SoTienDaThanhToan, 0)),
                OverdueRemaining = invoices
                    .Where(x => IsInvoiceOverdue(x, today))
                    .Sum(x => Math.Max(x.TongThanhToan - x.SoTienDaThanhToan, 0)),
                TotalAmount = invoices.Sum(x => x.TongThanhToan)
            };

            return View(viewModel);
        }

        private static AdminInvoiceManagementItemViewModel BuildInvoiceItem(HoaDon invoice, DateOnly today)
        {
            var booking = invoice.MaDatPhongNavigation;
            var customer = booking.MaKhNavigation;
            var employee = booking.MaNvNavigation;
            var remaining = Math.Max(invoice.TongThanhToan - invoice.SoTienDaThanhToan, 0);
            var isOverdue = IsInvoiceOverdue(invoice, today);
            var (statusLabel, statusClass) = GetInvoiceStatus(invoice, isOverdue);
            var displayDate = invoice.NgayThanhToanCuoi ?? booking.NgayDat;

            return new AdminInvoiceManagementItemViewModel
            {
                InvoiceCode = invoice.MaHoaDon.Trim(),
                BookingCode = invoice.MaDatPhong.Trim(),
                CustomerName = !string.IsNullOrWhiteSpace(booking.TenKhSnapshot)
                    ? booking.TenKhSnapshot.Trim()
                    : customer?.HoTen.Trim() ?? "Khách hàng",
                CustomerPhone = booking.SdtSnapshot ?? customer?.SoDienThoai,
                EmployeeId = booking.MaNv.Trim(),
                EmployeeName = employee?.HoTen.Trim() ?? "Chưa có nhân viên",
                EmployeePosition = employee?.ChucVu?.Trim() ?? string.Empty,
                RoomSummary = BuildRoomSummary(invoice),
                CreatedDateLabel = displayDate.ToString("dd/MM/yyyy", CultureInfo.GetCultureInfo("vi-VN")),
                CheckInLabel = booking.NgayNhanPhong.ToString("dd/MM/yyyy", CultureInfo.GetCultureInfo("vi-VN")),
                CheckOutLabel = booking.NgayTraPhong.ToString("dd/MM/yyyy", CultureInfo.GetCultureInfo("vi-VN")),
                SortDateValue = displayDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                RoomAmountLabel = FormatCurrency(invoice.TongTienPhong),
                ServiceAmountLabel = FormatCurrency(invoice.TongTienDichVu),
                DiscountAmountLabel = FormatCurrency(invoice.TienGiamGiaPhong),
                TotalAmountLabel = FormatCurrency(invoice.TongThanhToan),
                PaidAmountLabel = FormatCurrency(invoice.SoTienDaThanhToan),
                RemainingAmountLabel = FormatCurrency(remaining),
                PaymentMethod = string.IsNullOrWhiteSpace(invoice.PhuongThucThanhToan)
                    ? "Chưa có"
                    : invoice.PhuongThucThanhToan.Trim(),
                PaidDateLabel = invoice.NgayThanhToanCuoi.HasValue
                    ? invoice.NgayThanhToanCuoi.Value.ToString("dd/MM/yyyy HH:mm", CultureInfo.GetCultureInfo("vi-VN"))
                    : "Chưa thanh toán",
                StatusLabel = statusLabel,
                StatusClass = statusClass,
                IsOverdue = isOverdue,
                LineItems = invoice.ChiTietHoaDons
                    .Where(x => x.TrangThai == DomainValues.ChiTietHoaDonTrangThai.HieuLuc)
                    .OrderBy(x => x.NgayApDung)
                    .ThenBy(x => x.LoaiMuc)
                    .Select(BuildInvoiceLineItem)
                    .ToList()
            };
        }

        private static AdminInvoiceDetailLineItemViewModel BuildInvoiceLineItem(ChiTietHoaDon item)
        {
            return new AdminInvoiceDetailLineItemViewModel
            {
                TypeLabel = item.LoaiMuc == DomainValues.ChiTietHoaDonLoaiMuc.Phong ? "Phòng" : "Dịch vụ",
                Description = !string.IsNullOrWhiteSpace(item.NoiDung)
                    ? item.NoiDung.Trim()
                    : item.MaDvNavigation?.TenDv.Trim() ?? item.MaLoaiPhongNavigation?.TenLoaiPhong.Trim() ?? "Chi tiết",
                RoomCode = item.MaPhongNavigation?.SoPhong.Trim() ??
                           item.MaLoaiPhongNavigation?.TenLoaiPhong.Trim() ??
                           "Không áp dụng",
                DateLabel = item.NgayApDung?.ToString("dd/MM/yyyy", CultureInfo.GetCultureInfo("vi-VN")) ?? "-",
                Quantity = item.SoLuong,
                UnitPriceLabel = FormatCurrency(item.DonGia),
                AmountLabel = FormatCurrency(item.ThanhTien)
            };
        }

        private static string BuildRoomSummary(HoaDon invoice)
        {
            var roomLines = invoice.ChiTietHoaDons
                .Where(x => x.LoaiMuc == DomainValues.ChiTietHoaDonLoaiMuc.Phong &&
                            x.TrangThai == DomainValues.ChiTietHoaDonTrangThai.HieuLuc)
                .ToList();
            var assignedRooms = roomLines
                .Where(x => x.MaPhongNavigation != null)
                .Select(x => x.MaPhongNavigation!.SoPhong.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(x => x)
                .ToList();

            if (assignedRooms.Count > 0)
            {
                return string.Join(", ", assignedRooms);
            }

            var byRoomType = roomLines
                .Where(x => x.MaLoaiPhongNavigation != null)
                .GroupBy(x => x.MaLoaiPhongNavigation!.TenLoaiPhong)
                .Select(x => $"{x.Key} x{x.Sum(item => Math.Max(1, item.SoLuong))}")
                .ToList();

            return byRoomType.Count > 0 ? string.Join(", ", byRoomType) : "Chưa có phòng";
        }

        private static bool IsInvoiceOverdue(HoaDon invoice, DateOnly today)
        {
            if (invoice.TrangThai == DomainValues.HoaDonTrangThai.DaThanhToan ||
                invoice.TrangThai == DomainValues.HoaDonTrangThai.DaHuy ||
                invoice.SoTienDaThanhToan >= invoice.TongThanhToan)
            {
                return false;
            }

            return invoice.MaDatPhongNavigation.NgayTraPhong < today;
        }

        private static (string Label, string ClassName) GetInvoiceStatus(HoaDon invoice, bool isOverdue)
        {
            if (invoice.TrangThai == DomainValues.HoaDonTrangThai.DaHuy)
            {
                return ("Đã hủy", "cancelled");
            }

            if (isOverdue)
            {
                return ("Quá hạn", "overdue");
            }

            if (invoice.TrangThai == DomainValues.HoaDonTrangThai.DaThanhToan ||
                invoice.SoTienDaThanhToan >= invoice.TongThanhToan && invoice.TongThanhToan > 0)
            {
                return ("Đã thanh toán", "paid");
            }

            if (invoice.TrangThai == DomainValues.HoaDonTrangThai.ThanhToanMotPhan ||
                invoice.SoTienDaThanhToan > 0)
            {
                return ("Thanh toán một phần", "partial");
            }

            return ("Chưa thanh toán", "unpaid");
        }

        private static string FormatCurrency(decimal value)
        {
            return $"{value.ToString("N0", CultureInfo.GetCultureInfo("vi-VN"))} đ";
        }
    }
}
