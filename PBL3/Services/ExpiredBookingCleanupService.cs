using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using PBL3.Data;
using PBL3.Models;
using PBL3.Services.Interfaces;

namespace PBL3.Services;

public class ExpiredBookingCleanupService : IExpiredBookingCleanupService
{
    private const string WalkInVnPayMarker = "WALKIN_VNPAY_PENDING";

    private static readonly string[] NoShowCandidateStatuses =
    {
        DomainValues.DatPhongTrangThai.GiuCho,
        DomainValues.DatPhongTrangThai.DaDatCoc
    };

    private static readonly string[] PaidInvoiceStatuses =
    {
        DomainValues.HoaDonTrangThai.DaThanhToan,
        DomainValues.HoaDonTrangThai.ThanhToanMotPhan
    };

    private readonly ApplicationDbContext _context;
    private readonly VnPayOptions _options;
    private readonly ILogger<ExpiredBookingCleanupService> _logger;

    public ExpiredBookingCleanupService(
        ApplicationDbContext context,
        IOptions<VnPayOptions> options,
        ILogger<ExpiredBookingCleanupService> logger)
    {
        _context = context;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<int> CancelExpiredOnlinePaymentsAsync(CancellationToken cancellationToken = default)
    {
        var expireMinutes = Math.Clamp(_options.ExpireMinutes, 1, 1440);
        var cutoff = DateTime.UtcNow.AddMinutes(-expireMinutes);

        var expiredBookings = await _context.DatPhongs
            .Include(x => x.HoaDon)
            .ThenInclude(x => x!.ChiTietHoaDons)
            .AsSplitQuery()
            .Where(x => x.TrangThai == DomainValues.DatPhongTrangThai.GiuCho &&
                        x.NgayDat <= cutoff &&
                        x.HoaDon != null &&
                        x.HoaDon.TrangThai == DomainValues.HoaDonTrangThai.ChuaThanhToan &&
                        x.HoaDon.PhuongThucThanhToan == DomainValues.PhuongThucThanhToan.Qr)
            .ToListAsync(cancellationToken);

        if (expiredBookings.Count == 0)
        {
            return 0;
        }

        foreach (var booking in expiredBookings)
        {
            booking.TrangThai = DomainValues.DatPhongTrangThai.DaHuy;
            booking.GhiChu = AppendNote(booking.GhiChu, $"Tu huy do qua han thanh toan VNPay sau {expireMinutes} phut.");

            if (booking.HoaDon != null)
            {
                booking.HoaDon.TrangThai = DomainValues.HoaDonTrangThai.DaHuy;
                booking.HoaDon.GhiChu = AppendNote(booking.HoaDon.GhiChu, $"Tu huy do qua han thanh toan VNPay sau {expireMinutes} phut.");
            }
        }

        var walkInRoomIds = expiredBookings
            .Where(x => x.HoaDon?.GhiChu?.Contains(WalkInVnPayMarker, StringComparison.OrdinalIgnoreCase) == true)
            .SelectMany(x => x.HoaDon?.ChiTietHoaDons.AsEnumerable() ?? Enumerable.Empty<ChiTietHoaDon>())
            .Where(x => x.LoaiMuc == DomainValues.ChiTietHoaDonLoaiMuc.Phong &&
                        !string.IsNullOrWhiteSpace(x.MaPhong))
            .Select(x => x.MaPhong!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (walkInRoomIds.Count > 0)
        {
            var rooms = await _context.Phongs
                .Where(x => walkInRoomIds.Contains(x.MaPhong) &&
                            x.TrangThai == DomainValues.PhongTrangThai.DangSuDung)
                .ToListAsync(cancellationToken);
            foreach (var room in rooms)
            {
                room.TrangThai = DomainValues.PhongTrangThai.Trong;
            }
        }

        await _context.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Cancelled {Count} expired VNPay booking holds.", expiredBookings.Count);
        return expiredBookings.Count;
    }

    public async Task<int> MarkExpiredNoShowBookingsAsync(CancellationToken cancellationToken = default)
    {
        var today = DateOnly.FromDateTime(GetVietnamNow());

        var expiredBookings = await _context.DatPhongs
            .Include(x => x.HoaDon)
            .ThenInclude(x => x!.ChiTietHoaDons)
            .AsSplitQuery()
            .Where(x => NoShowCandidateStatuses.Contains(x.TrangThai) &&
                        x.NgayNhanPhong < today &&
                        x.HoaDon != null &&
                        PaidInvoiceStatuses.Contains(x.HoaDon.TrangThai))
            .ToListAsync(cancellationToken);

        if (expiredBookings.Count == 0)
        {
            return 0;
        }

        var expiredBookingIds = expiredBookings
            .Select(x => x.MaDatPhong)
            .ToList();

        var assignedRoomIds = expiredBookings
            .SelectMany(x => x.HoaDon?.ChiTietHoaDons.AsEnumerable() ?? Enumerable.Empty<ChiTietHoaDon>())
            .Where(x => x.LoaiMuc == DomainValues.ChiTietHoaDonLoaiMuc.Phong &&
                        x.TrangThai == DomainValues.ChiTietHoaDonTrangThai.HieuLuc &&
                        !string.IsNullOrWhiteSpace(x.MaPhong))
            .Select(x => x.MaPhong!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        foreach (var booking in expiredBookings)
        {
            booking.TrangThai = DomainValues.DatPhongTrangThai.QuaHanNhanPhong;
            booking.GhiChu = AppendNote(
                booking.GhiChu,
                "Tu dong danh dau qua han nhan phong do khach khong den.");
        }

        if (assignedRoomIds.Count > 0)
        {
            await ReleaseRoomsIfNotBlockedAsync(assignedRoomIds, expiredBookingIds, cancellationToken);
        }

        await _context.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Marked {Count} bookings as no-show after missed check-in date.", expiredBookings.Count);
        return expiredBookings.Count;
    }

    private async Task ReleaseRoomsIfNotBlockedAsync(
        IReadOnlyCollection<string> roomIds,
        IReadOnlyCollection<string> expiredBookingIds,
        CancellationToken cancellationToken)
    {
        var blockedRoomIds = await _context.ChiTietHoaDons
            .AsNoTracking()
            .Include(x => x.MaHoaDonNavigation)
            .ThenInclude(x => x.MaDatPhongNavigation)
            .Where(x => x.LoaiMuc == DomainValues.ChiTietHoaDonLoaiMuc.Phong &&
                        x.TrangThai == DomainValues.ChiTietHoaDonTrangThai.HieuLuc &&
                        x.MaPhong != null &&
                        roomIds.Contains(x.MaPhong) &&
                        !expiredBookingIds.Contains(x.MaHoaDonNavigation.MaDatPhong) &&
                        x.MaHoaDonNavigation.MaDatPhongNavigation.TrangThai == DomainValues.DatPhongTrangThai.DaNhanPhong)
            .Select(x => x.MaPhong!)
            .Distinct()
            .ToListAsync(cancellationToken);

        var blockedRoomIdSet = blockedRoomIds.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var roomsToRelease = await _context.Phongs
            .Where(x => roomIds.Contains(x.MaPhong) &&
                        x.TrangThai == DomainValues.PhongTrangThai.DangSuDung)
            .ToListAsync(cancellationToken);

        foreach (var room in roomsToRelease)
        {
            if (!blockedRoomIdSet.Contains(room.MaPhong))
            {
                room.TrangThai = DomainValues.PhongTrangThai.Trong;
            }
        }
    }

    private static DateTime GetVietnamNow()
    {
        return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, GetVietnamTimeZone());
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

    private static string AppendNote(string? current, string next)
    {
        if (string.IsNullOrWhiteSpace(current))
        {
            return next.Length <= 255 ? next : next[..255];
        }

        var combined = $"{current}; {next}";
        return combined.Length <= 255 ? combined : combined[..255];
    }
}
