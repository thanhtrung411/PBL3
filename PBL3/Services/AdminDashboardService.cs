using Microsoft.EntityFrameworkCore;
using PBL3.Data;
using PBL3.Models;
using PBL3.Services.Interfaces;

namespace PBL3.Services;

public sealed class AdminDashboardService : IAdminDashboardService
{
    private static readonly string[] ActiveRoomBookingStatuses =
    {
        DomainValues.DatPhongTrangThai.GiuCho,
        DomainValues.DatPhongTrangThai.DaNhanPhong
    };

    private readonly ApplicationDbContext _context;

    public AdminDashboardService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<AdminDashboardViewModel> GetOverviewAsync(CancellationToken cancellationToken = default)
    {
        var vietnamNow = GetVietnamNow();
        var vietnamToday = DateOnly.FromDateTime(vietnamNow);
        var todayStartUtc = ToUtc(vietnamNow.Date);
        var todayEndUtc = ToUtc(vietnamNow.Date.AddDays(1));
        var monthStartLocal = new DateTime(vietnamNow.Year, vietnamNow.Month, 1).AddMonths(-5);
        var nextMonthStartLocal = new DateTime(vietnamNow.Year, vietnamNow.Month, 1).AddMonths(1);
        var monthStartUtc = ToUtc(monthStartLocal);
        var nextMonthStartUtc = ToUtc(nextMonthStartLocal);

        var totalRooms = await _context.Phongs
            .AsNoTracking()
            .CountAsync(cancellationToken);

        var operationalRooms = await _context.Phongs
            .AsNoTracking()
            .CountAsync(x => x.TrangThai != DomainValues.PhongTrangThai.NgungSuDung, cancellationToken);

        var occupiedRooms = await _context.Phongs
            .AsNoTracking()
            .CountAsync(x => x.TrangThai == DomainValues.PhongTrangThai.DangSuDung, cancellationToken);

        var todayRevenue = await _context.HoaDons
            .AsNoTracking()
            .Where(x => x.TrangThai == DomainValues.HoaDonTrangThai.DaThanhToan &&
                        x.NgayThanhToanCuoi >= todayStartUtc &&
                        x.NgayThanhToanCuoi < todayEndUtc)
            .SumAsync(x => (decimal?)x.SoTienDaThanhToan, cancellationToken) ?? 0m;

        var monthlyRevenueRows = await _context.HoaDons
            .AsNoTracking()
            .Where(x => x.TrangThai == DomainValues.HoaDonTrangThai.DaThanhToan &&
                        x.NgayThanhToanCuoi >= monthStartUtc &&
                        x.NgayThanhToanCuoi < nextMonthStartUtc)
            .Select(x => new
            {
                PaidAt = x.NgayThanhToanCuoi!.Value,
                Amount = x.SoTienDaThanhToan
            })
            .ToListAsync(cancellationToken);

        var revenueByMonth = monthlyRevenueRows
            .GroupBy(x =>
            {
                var localPaidAt = TimeZoneInfo.ConvertTimeFromUtc(
                    DateTime.SpecifyKind(x.PaidAt, DateTimeKind.Utc),
                    GetVietnamTimeZone());
                return new DateTime(localPaidAt.Year, localPaidAt.Month, 1);
            })
            .ToDictionary(x => x.Key, x => x.Sum(item => item.Amount));

        var monthBuckets = Enumerable.Range(0, 6)
            .Select(offset => monthStartLocal.AddMonths(offset))
            .ToList();

        var roomStatusRows = await _context.Phongs
            .AsNoTracking()
            .GroupBy(x => x.TrangThai)
            .Select(x => new RoomStatusCount { Status = x.Key, Count = x.Count() })
            .ToListAsync(cancellationToken);

        var recentBookingEntities = await _context.DatPhongs
            .AsNoTracking()
            .Include(x => x.HoaDon)
                .ThenInclude(x => x!.ChiTietHoaDons)
                    .ThenInclude(x => x.MaPhongNavigation)
            .Include(x => x.HoaDon)
                .ThenInclude(x => x!.ChiTietHoaDons)
                    .ThenInclude(x => x.MaLoaiPhongNavigation)
            .OrderByDescending(x => x.NgayDat)
            .Take(5)
            .ToListAsync(cancellationToken);

        return new AdminDashboardViewModel
        {
            TotalRooms = totalRooms,
            OperationalRooms = operationalRooms,
            OccupiedRooms = occupiedRooms,
            TodayRevenue = todayRevenue,
            OccupancyRate = operationalRooms == 0 ? 0 : Math.Round(occupiedRooms * 100d / operationalRooms, 1),
            GeneratedAt = vietnamNow,
            MonthlyRevenueLabels = monthBuckets.Select(x => $"T{x.Month}").ToList(),
            MonthlyRevenueValues = monthBuckets
                .Select(x => revenueByMonth.GetValueOrDefault(new DateTime(x.Year, x.Month, 1), 0m))
                .ToList(),
            RoomStatuses = BuildRoomStatuses(roomStatusRows),
            RecentBookings = recentBookingEntities.Select(BuildRecentBooking).ToList()
        };
    }

    private static IReadOnlyList<RoomStatusDashboardItem> BuildRoomStatuses(
        IReadOnlyCollection<RoomStatusCount> roomStatusRows)
    {
        int Count(string status)
        {
            return roomStatusRows
                .Where(x => string.Equals(x.Status, status, StringComparison.Ordinal))
                .Sum(x => x.Count);
        }

        var used = Count(DomainValues.PhongTrangThai.DangSuDung);
        var empty = Count(DomainValues.PhongTrangThai.Trong);
        var maintenance = Count(DomainValues.PhongTrangThai.BaoTri);
        var disabled = Count(DomainValues.PhongTrangThai.NgungSuDung);

        return new[]
        {
            new RoomStatusDashboardItem
            {
                Label = "Đang sử dụng",
                Count = used,
                Color = "#4e73df",
                LegendClass = "text-primary"
            },
            new RoomStatusDashboardItem
            {
                Label = "Trống",
                Count = empty,
                Color = "#1cc88a",
                LegendClass = "text-success"
            },
            new RoomStatusDashboardItem
            {
                Label = "Bảo trì",
                Count = maintenance,
                Color = "#f6c23e",
                LegendClass = "text-warning"
            },
            new RoomStatusDashboardItem
            {
                Label = "Ngưng sử dụng",
                Count = disabled,
                Color = "#858796",
                LegendClass = "text-secondary"
            }
        };
    }

    private static RecentBookingDashboardItem BuildRecentBooking(DatPhong booking)
    {
        var roomDetails = booking.HoaDon?.ChiTietHoaDons
            .Where(x => x.LoaiMuc == DomainValues.ChiTietHoaDonLoaiMuc.Phong &&
                        x.TrangThai == DomainValues.ChiTietHoaDonTrangThai.HieuLuc)
            .ToList() ?? new List<ChiTietHoaDon>();

        var roomSummary = BuildRoomSummary(roomDetails);
        var nights = Math.Max(1, booking.NgayTraPhong.DayNumber - booking.NgayNhanPhong.DayNumber);
        var (statusLabel, statusClass) = GetBookingStatus(booking);

        return new RecentBookingDashboardItem
        {
            BookingCode = booking.MaDatPhong.Trim(),
            CustomerName = booking.TenKhSnapshot,
            CustomerPhone = booking.SdtSnapshot,
            RoomSummary = roomSummary,
            CheckInDate = booking.NgayNhanPhong,
            Nights = nights,
            StatusLabel = statusLabel,
            StatusClass = statusClass
        };
    }

    private static string BuildRoomSummary(IReadOnlyCollection<ChiTietHoaDon> details)
    {
        var assignedRooms = details
            .Where(x => x.MaPhongNavigation != null)
            .Select(x => x.MaPhongNavigation!.SoPhong.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x)
            .ToList();

        if (assignedRooms.Count > 0)
        {
            return string.Join(", ", assignedRooms);
        }

        var byType = details
            .Where(x => x.MaLoaiPhongNavigation != null)
            .GroupBy(x => x.MaLoaiPhongNavigation!.TenLoaiPhong)
            .Select(x => $"{x.Key} x {x.Sum(item => item.SoLuong)}")
            .ToList();

        return byType.Count > 0 ? string.Join(", ", byType) : "Chưa gán phòng";
    }

    private static (string Label, string ClassName) GetBookingStatus(DatPhong booking)
    {
        if (booking.TrangThai == DomainValues.DatPhongTrangThai.DaHuy)
        {
            return ("Đã hủy", "badge-occupied");
        }

        if (booking.TrangThai == DomainValues.DatPhongTrangThai.QuaHanNhanPhong)
        {
            return ("Quá hạn nhận phòng", "badge-occupied");
        }

        if (booking.TrangThai == DomainValues.DatPhongTrangThai.TraPhong)
        {
            return ("Đã trả phòng", "badge-empty");
        }

        if (booking.TrangThai == DomainValues.DatPhongTrangThai.DaNhanPhong)
        {
            return ("Đã nhận phòng", "badge-empty");
        }

        if (booking.HoaDon?.TrangThai == DomainValues.HoaDonTrangThai.DaThanhToan)
        {
            return ("Đã thanh toán", "badge-booked");
        }

        if (ActiveRoomBookingStatuses.Contains(booking.TrangThai))
        {
            return ("Giữ chỗ", "badge-maintenance");
        }

        return (booking.TrangThai, "badge-booked");
    }

    private static DateTime GetVietnamNow()
    {
        return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, GetVietnamTimeZone());
    }

    private static DateTime ToUtc(DateTime localVietnamTime)
    {
        return TimeZoneInfo.ConvertTimeToUtc(
            DateTime.SpecifyKind(localVietnamTime, DateTimeKind.Unspecified),
            GetVietnamTimeZone());
    }

    private static TimeZoneInfo GetVietnamTimeZone()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
        }
        catch (Exception ex) when (ex is TimeZoneNotFoundException or InvalidTimeZoneException)
        {
            return TimeZoneInfo.FindSystemTimeZoneById("Asia/Ho_Chi_Minh");
        }
    }

    private sealed class RoomStatusCount
    {
        public string Status { get; set; } = "";

        public int Count { get; set; }
    }
}
