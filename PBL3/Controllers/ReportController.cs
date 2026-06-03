using System.Globalization;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PBL3.Data;
using PBL3.Models;

namespace PBL3.Controllers
{
    public class ReportController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ReportController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(string? month, CancellationToken cancellationToken = default)
        {
            var periodStart = ParseMonthStart(month);
            var model = await BuildReportAsync(periodStart, cancellationToken);
            return View(model);
        }

        public async Task<IActionResult> Export(string? month, CancellationToken cancellationToken = default)
        {
            var periodStart = ParseMonthStart(month);
            var model = await BuildReportAsync(periodStart, cancellationToken);
            var csv = BuildCsvReport(model);
            var utf8Bom = Encoding.UTF8.GetPreamble();
            var csvBytes = Encoding.UTF8.GetBytes(csv);
            var bytes = new byte[utf8Bom.Length + csvBytes.Length];
            Buffer.BlockCopy(utf8Bom, 0, bytes, 0, utf8Bom.Length);
            Buffer.BlockCopy(csvBytes, 0, bytes, utf8Bom.Length, csvBytes.Length);

            return File(bytes, "text/csv; charset=utf-8", $"bao-cao-{model.SelectedMonthValue}.csv");
        }

        private async Task<AdminReportViewModel> BuildReportAsync(
            DateOnly periodStart,
            CancellationToken cancellationToken)
        {
            var periodEnd = periodStart.AddMonths(1).AddDays(-1);
            var nextPeriodStart = periodEnd.AddDays(1);
            var periodStartDateTime = periodStart.ToDateTime(TimeOnly.MinValue);
            var nextPeriodStartDateTime = nextPeriodStart.ToDateTime(TimeOnly.MinValue);

            var invoicesInPeriod = await _context.HoaDons
                .AsNoTracking()
                .Where(x => x.TrangThai != DomainValues.HoaDonTrangThai.DaHuy &&
                            x.NgayThanhToanCuoi != null &&
                            x.NgayThanhToanCuoi >= periodStartDateTime &&
                            x.NgayThanhToanCuoi < nextPeriodStartDateTime)
                .Select(x => new
                {
                    x.SoTienDaThanhToan,
                    x.TongThanhToan
                })
                .ToListAsync(cancellationToken);

            var bookingsInPeriod = await _context.DatPhongs
                .AsNoTracking()
                .Where(x => x.NgayDat >= periodStartDateTime &&
                            x.NgayDat < nextPeriodStartDateTime &&
                            x.TrangThai != DomainValues.DatPhongTrangThai.DaHuy &&
                            x.TrangThai != DomainValues.DatPhongTrangThai.QuaHanNhanPhong)
                .CountAsync(cancellationToken);

            var totalRooms = await _context.Phongs
                .AsNoTracking()
                .CountAsync(x => x.TrangThai != DomainValues.PhongTrangThai.NgungSuDung, cancellationToken);

            var revenueTrendStart = periodStart.AddMonths(-5);
            var revenueTrendEnd = periodStart.AddMonths(1);
            var revenueRows = await _context.HoaDons
                .AsNoTracking()
                .Where(x => x.TrangThai != DomainValues.HoaDonTrangThai.DaHuy &&
                            x.NgayThanhToanCuoi != null &&
                            x.NgayThanhToanCuoi >= revenueTrendStart.ToDateTime(TimeOnly.MinValue) &&
                            x.NgayThanhToanCuoi < revenueTrendEnd.ToDateTime(TimeOnly.MinValue))
                .Select(x => new
                {
                    PaidAt = x.NgayThanhToanCuoi!.Value,
                    Amount = x.SoTienDaThanhToan
                })
                .ToListAsync(cancellationToken);

            var revenueByMonth = revenueRows
                .GroupBy(x => new DateOnly(x.PaidAt.Year, x.PaidAt.Month, 1))
                .ToDictionary(x => x.Key, x => x.Sum(item => item.Amount));

            var monthBuckets = Enumerable.Range(0, 6)
                .Select(offset => revenueTrendStart.AddMonths(offset))
                .ToList();

            var roomTypeRevenueRows = await _context.ChiTietHoaDons
                .AsNoTracking()
                .Include(x => x.MaHoaDonNavigation)
                .Include(x => x.MaLoaiPhongNavigation)
                .Where(x => x.LoaiMuc == DomainValues.ChiTietHoaDonLoaiMuc.Phong &&
                            x.TrangThai == DomainValues.ChiTietHoaDonTrangThai.HieuLuc &&
                            x.MaHoaDonNavigation.TrangThai != DomainValues.HoaDonTrangThai.DaHuy &&
                            x.MaHoaDonNavigation.NgayThanhToanCuoi != null &&
                            x.MaHoaDonNavigation.NgayThanhToanCuoi >= periodStartDateTime &&
                            x.MaHoaDonNavigation.NgayThanhToanCuoi < nextPeriodStartDateTime)
                .Select(x => new
                {
                    RoomType = x.MaLoaiPhongNavigation != null
                        ? x.MaLoaiPhongNavigation.TenLoaiPhong
                        : x.MaLoaiPhong ?? "Khác",
                    Amount = x.ThanhTien
                })
                .ToListAsync(cancellationToken);

            var roomTypeRevenue = roomTypeRevenueRows
                .GroupBy(x => x.RoomType)
                .OrderByDescending(x => x.Sum(item => item.Amount))
                .ToList();

            var occupancyBookings = await _context.DatPhongs
                .AsNoTracking()
                .AsSplitQuery()
                .Include(x => x.HoaDon)
                .ThenInclude(x => x!.ChiTietHoaDons)
                .Where(x => x.TrangThai != DomainValues.DatPhongTrangThai.DaHuy &&
                            x.TrangThai != DomainValues.DatPhongTrangThai.QuaHanNhanPhong &&
                            x.HoaDon != null &&
                            x.NgayNhanPhong < revenueTrendEnd &&
                            x.NgayTraPhong > revenueTrendStart)
                .ToListAsync(cancellationToken);

            var occupancyByMonth = monthBuckets
                .ToDictionary(
                    x => x,
                    x => CalculateOccupancyRate(occupancyBookings, x, x.AddMonths(1), totalRooms));

            var totalRevenue = invoicesInPeriod.Sum(x => x.SoTienDaThanhToan);
            var averageOrderValue = invoicesInPeriod.Count == 0
                ? 0
                : invoicesInPeriod.Average(x => x.TongThanhToan);

            return new AdminReportViewModel
            {
                PeriodStart = periodStart,
                PeriodEnd = periodEnd,
                PeriodLabel = $"{FormatDate(periodStart)} - {FormatDate(periodEnd)}",
                SelectedMonthValue = periodStart.ToString("yyyy-MM", CultureInfo.InvariantCulture),
                TotalRevenue = totalRevenue,
                TotalBookings = bookingsInPeriod,
                OccupancyRate = CalculateOccupancyRate(occupancyBookings, periodStart, nextPeriodStart, totalRooms),
                AverageOrderValue = averageOrderValue,
                RevenueTrendLabels = monthBuckets.Select(x => $"T{x.Month}/{x.Year}").ToList(),
                RevenueTrendValues = monthBuckets.Select(x => revenueByMonth.GetValueOrDefault(x, 0m)).ToList(),
                RoomTypeRevenueLabels = roomTypeRevenue.Select(x => x.Key).ToList(),
                RoomTypeRevenueValues = roomTypeRevenue.Select(x => x.Sum(item => item.Amount)).ToList(),
                OccupancyLabels = monthBuckets.Select(x => $"T{x.Month}/{x.Year}").ToList(),
                OccupancyValues = monthBuckets.Select(x => occupancyByMonth.GetValueOrDefault(x, 0d)).ToList()
            };
        }

        private static string BuildCsvReport(AdminReportViewModel model)
        {
            var builder = new StringBuilder();
            AppendCsvRow(builder, "Báo cáo tổng hợp", model.PeriodLabel);
            AppendCsvRow(builder);
            AppendCsvRow(builder, "Chỉ số", "Giá trị");
            AppendCsvRow(builder, "Tổng doanh thu", model.TotalRevenue.ToString(CultureInfo.InvariantCulture));
            AppendCsvRow(builder, "Tổng đặt phòng", model.TotalBookings.ToString(CultureInfo.InvariantCulture));
            AppendCsvRow(builder, "Tỷ lệ lấp đầy TB", $"{model.OccupancyRate.ToString("0.#", CultureInfo.InvariantCulture)}%");
            AppendCsvRow(builder, "Giá trị đơn TB", model.AverageOrderValue.ToString(CultureInfo.InvariantCulture));

            AppendCsvRow(builder);
            AppendCsvRow(builder, "Xu hướng doanh thu");
            AppendCsvRow(builder, "Tháng", "Doanh thu", "Tỷ lệ lấp đầy");
            for (var i = 0; i < model.RevenueTrendLabels.Count; i++)
            {
                var occupancy = i < model.OccupancyValues.Count ? model.OccupancyValues[i] : 0;
                AppendCsvRow(
                    builder,
                    model.RevenueTrendLabels[i],
                    model.RevenueTrendValues[i].ToString(CultureInfo.InvariantCulture),
                    $"{occupancy.ToString("0.#", CultureInfo.InvariantCulture)}%");
            }

            AppendCsvRow(builder);
            AppendCsvRow(builder, "Doanh thu theo loại phòng");
            AppendCsvRow(builder, "Loại phòng", "Doanh thu");
            for (var i = 0; i < model.RoomTypeRevenueLabels.Count; i++)
            {
                AppendCsvRow(
                    builder,
                    model.RoomTypeRevenueLabels[i],
                    model.RoomTypeRevenueValues[i].ToString(CultureInfo.InvariantCulture));
            }

            return builder.ToString();
        }

        private static void AppendCsvRow(StringBuilder builder, params object[] values)
        {
            builder.AppendLine(string.Join(";", values.Select(value => EscapeCsv(value?.ToString() ?? string.Empty))));
        }

        private static string EscapeCsv(string value)
        {
            if (!value.Contains(';') && !value.Contains('"') && !value.Contains('\n') && !value.Contains('\r'))
            {
                return value;
            }

            return $"\"{value.Replace("\"", "\"\"")}\"";
        }

        private static DateOnly ParseMonthStart(string? month)
        {
            if (DateTime.TryParseExact(
                    month,
                    "yyyy-MM",
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out var parsed))
            {
                return new DateOnly(parsed.Year, parsed.Month, 1);
            }

            var today = DateOnly.FromDateTime(DateTime.Today);
            return new DateOnly(today.Year, today.Month, 1);
        }

        private static double CalculateOccupancyRate(
            IReadOnlyCollection<DatPhong> bookings,
            DateOnly periodStart,
            DateOnly periodEnd,
            int totalRooms)
        {
            var totalDays = Math.Max(periodEnd.DayNumber - periodStart.DayNumber, 1);
            if (totalRooms <= 0)
            {
                return 0;
            }

            var occupiedRoomNights = bookings.Sum(booking =>
            {
                var overlapStart = booking.NgayNhanPhong > periodStart ? booking.NgayNhanPhong : periodStart;
                var overlapEnd = booking.NgayTraPhong < periodEnd ? booking.NgayTraPhong : periodEnd;
                var nights = Math.Max(overlapEnd.DayNumber - overlapStart.DayNumber, 0);
                if (nights <= 0)
                {
                    return 0;
                }

                var roomCount = booking.HoaDon?.ChiTietHoaDons
                    .Count(x => x.LoaiMuc == DomainValues.ChiTietHoaDonLoaiMuc.Phong &&
                                x.TrangThai == DomainValues.ChiTietHoaDonTrangThai.HieuLuc) ?? 0;

                return nights * Math.Max(roomCount, 1);
            });

            return Math.Round(Math.Min(100d, occupiedRoomNights * 100d / (totalRooms * totalDays)), 1);
        }

        private static string FormatDate(DateOnly date)
        {
            return date.ToString("dd/MM/yyyy", CultureInfo.GetCultureInfo("vi-VN"));
        }
    }
}
