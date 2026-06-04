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
            var invoicesData = await _context.HoaDons
                .AsNoTracking()
                .OrderByDescending(x => x.NgayThanhToanCuoi ?? x.MaDatPhongNavigation.NgayDat)
                .Take(300)
                .Select(x => new
                {
                    x.MaHoaDon,
                    x.MaDatPhong,
                    x.TongTienPhong,
                    x.TongTienDichVu,
                    x.TienGiamGiaPhong,
                    x.TongThanhToan,
                    x.SoTienDaThanhToan,
                    x.TrangThai,
                    x.PhuongThucThanhToan,
                    x.NgayThanhToanCuoi,
                    BookingTenKhSnapshot = x.MaDatPhongNavigation.TenKhSnapshot,
                    BookingSdtSnapshot = x.MaDatPhongNavigation.SdtSnapshot,
                    BookingNgayDat = x.MaDatPhongNavigation.NgayDat,
                    BookingNgayNhanPhong = x.MaDatPhongNavigation.NgayNhanPhong,
                    BookingNgayTraPhong = x.MaDatPhongNavigation.NgayTraPhong,
                    BookingMaNv = x.MaDatPhongNavigation.MaNv,
                    CustomerName = x.MaDatPhongNavigation.MaKhNavigation != null
                        ? x.MaDatPhongNavigation.MaKhNavigation.HoTen : null,
                    CustomerPhone = x.MaDatPhongNavigation.MaKhNavigation != null
                        ? x.MaDatPhongNavigation.MaKhNavigation.SoDienThoai : null,
                    EmployeeName = x.MaDatPhongNavigation.MaNvNavigation != null
                        ? x.MaDatPhongNavigation.MaNvNavigation.HoTen : null,
                    EmployeePosition = x.MaDatPhongNavigation.MaNvNavigation != null
                        ? x.MaDatPhongNavigation.MaNvNavigation.ChucVu : null,
                    LineItems = x.ChiTietHoaDons
                        .Where(ct => ct.TrangThai == DomainValues.ChiTietHoaDonTrangThai.HieuLuc)
                        .OrderBy(ct => ct.NgayApDung)
                        .ThenBy(ct => ct.LoaiMuc)
                        .Select(ct => new
                        {
                            ct.LoaiMuc,
                            ct.NoiDung,
                            ct.SoLuong,
                            ct.DonGia,
                            ct.ThanhTien,
                            ct.NgayApDung,
                            RoomNumber = ct.MaPhongNavigation != null ? ct.MaPhongNavigation.SoPhong : null,
                            RoomTypeName = ct.MaLoaiPhongNavigation != null ? ct.MaLoaiPhongNavigation.TenLoaiPhong : null,
                            ServiceName = ct.MaDvNavigation != null ? ct.MaDvNavigation.TenDv : null
                        })
                        .ToList(),
                    RoomSummaryData = x.ChiTietHoaDons
                        .Where(ct => ct.LoaiMuc == DomainValues.ChiTietHoaDonLoaiMuc.Phong &&
                                     ct.TrangThai == DomainValues.ChiTietHoaDonTrangThai.HieuLuc)
                        .Select(ct => new
                        {
                            RoomNumber = ct.MaPhongNavigation != null ? ct.MaPhongNavigation.SoPhong : null,
                            RoomTypeName = ct.MaLoaiPhongNavigation != null ? ct.MaLoaiPhongNavigation.TenLoaiPhong : null,
                            ct.SoLuong
                        })
                        .ToList()
                })
                .ToListAsync();

            var items = invoicesData.Select(invoice =>
            {
                var remaining = Math.Max(invoice.TongThanhToan - invoice.SoTienDaThanhToan, 0);
                var isOverdue = IsInvoiceOverdue(invoice.TrangThai, invoice.SoTienDaThanhToan,
                    invoice.TongThanhToan, invoice.BookingNgayTraPhong, today);
                var (statusLabel, statusClass) = GetInvoiceStatus(invoice.TrangThai, isOverdue,
                    invoice.SoTienDaThanhToan, invoice.TongThanhToan);
                var displayDate = invoice.NgayThanhToanCuoi ?? invoice.BookingNgayDat;

                // Room summary
                var assignedRooms = invoice.RoomSummaryData
                    .Where(r => r.RoomNumber != null)
                    .Select(r => r.RoomNumber!.Trim())
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(r => r)
                    .ToList();
                string roomSummary;
                if (assignedRooms.Count > 0)
                {
                    roomSummary = string.Join(", ", assignedRooms);
                }
                else
                {
                    var byRoomType = invoice.RoomSummaryData
                        .Where(r => r.RoomTypeName != null)
                        .GroupBy(r => r.RoomTypeName!)
                        .Select(g => $"{g.Key} x{g.Sum(item => Math.Max(1, item.SoLuong))}")
                        .ToList();
                    roomSummary = byRoomType.Count > 0 ? string.Join(", ", byRoomType) : "Chưa có phòng";
                }

                return new AdminInvoiceManagementItemViewModel
                {
                    InvoiceCode = invoice.MaHoaDon.Trim(),
                    BookingCode = invoice.MaDatPhong.Trim(),
                    CustomerName = !string.IsNullOrWhiteSpace(invoice.BookingTenKhSnapshot)
                        ? invoice.BookingTenKhSnapshot.Trim()
                        : invoice.CustomerName?.Trim() ?? "Khách hàng",
                    CustomerPhone = invoice.BookingSdtSnapshot ?? invoice.CustomerPhone,
                    EmployeeId = invoice.BookingMaNv.Trim(),
                    EmployeeName = invoice.EmployeeName?.Trim() ?? "Chưa có nhân viên",
                    EmployeePosition = invoice.EmployeePosition?.Trim() ?? string.Empty,
                    RoomSummary = roomSummary,
                    CreatedDateLabel = displayDate.ToString("dd/MM/yyyy", CultureInfo.GetCultureInfo("vi-VN")),
                    CheckInLabel = invoice.BookingNgayNhanPhong.ToString("dd/MM/yyyy", CultureInfo.GetCultureInfo("vi-VN")),
                    CheckOutLabel = invoice.BookingNgayTraPhong.ToString("dd/MM/yyyy", CultureInfo.GetCultureInfo("vi-VN")),
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
                    LineItems = invoice.LineItems.Select(item => new AdminInvoiceDetailLineItemViewModel
                    {
                        TypeLabel = item.LoaiMuc == DomainValues.ChiTietHoaDonLoaiMuc.Phong ? "Phòng" : "Dịch vụ",
                        Description = !string.IsNullOrWhiteSpace(item.NoiDung)
                            ? item.NoiDung.Trim()
                            : item.ServiceName?.Trim() ?? item.RoomTypeName?.Trim() ?? "Chi tiết",
                        RoomCode = item.RoomNumber?.Trim() ?? item.RoomTypeName?.Trim() ?? "Không áp dụng",
                        DateLabel = item.NgayApDung?.ToString("dd/MM/yyyy", CultureInfo.GetCultureInfo("vi-VN")) ?? "-",
                        Quantity = item.SoLuong,
                        UnitPriceLabel = FormatCurrency(item.DonGia),
                        AmountLabel = FormatCurrency(item.ThanhTien)
                    }).ToList()
                };
            }).ToList();

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
                TotalPaid = invoicesData.Sum(x => x.SoTienDaThanhToan),
                TotalRemaining = invoicesData.Sum(x => Math.Max(x.TongThanhToan - x.SoTienDaThanhToan, 0)),
                OverdueRemaining = invoicesData
                    .Where(x => IsInvoiceOverdue(x.TrangThai, x.SoTienDaThanhToan, x.TongThanhToan, x.BookingNgayTraPhong, today))
                    .Sum(x => Math.Max(x.TongThanhToan - x.SoTienDaThanhToan, 0)),
                TotalAmount = invoicesData.Sum(x => x.TongThanhToan)
            };

            return View(viewModel);
        }

        private static bool IsInvoiceOverdue(
            string trangThai, decimal soTienDaThanhToan, decimal tongThanhToan,
            DateOnly ngayTraPhong, DateOnly today)
        {
            if (trangThai == DomainValues.HoaDonTrangThai.DaThanhToan ||
                trangThai == DomainValues.HoaDonTrangThai.DaHuy ||
                soTienDaThanhToan >= tongThanhToan)
            {
                return false;
            }

            return ngayTraPhong < today;
        }

        private static (string Label, string ClassName) GetInvoiceStatus(
            string trangThai, bool isOverdue, decimal soTienDaThanhToan, decimal tongThanhToan)
        {
            if (trangThai == DomainValues.HoaDonTrangThai.DaHuy)
            {
                return ("Đã hủy", "cancelled");
            }

            if (isOverdue)
            {
                return ("Quá hạn", "overdue");
            }

            if (trangThai == DomainValues.HoaDonTrangThai.DaThanhToan ||
                soTienDaThanhToan >= tongThanhToan && tongThanhToan > 0)
            {
                return ("Đã thanh toán", "paid");
            }

            if (trangThai == DomainValues.HoaDonTrangThai.ThanhToanMotPhan ||
                soTienDaThanhToan > 0)
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
