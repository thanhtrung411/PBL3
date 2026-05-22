using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using PBL3.Data;
using PBL3.Models;
using PBL3.Services.Interfaces;

namespace PBL3.Services;

public class ExpiredBookingCleanupService : IExpiredBookingCleanupService
{
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

        await _context.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Cancelled {Count} expired VNPay booking holds.", expiredBookings.Count);
        return expiredBookings.Count;
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
