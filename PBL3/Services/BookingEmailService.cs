using System.Globalization;
using System.Net;
using Microsoft.EntityFrameworkCore;
using PBL3.Data;
using PBL3.Models;
using PBL3.Services.Interfaces;

namespace PBL3.Services;

public class BookingEmailService : IBookingEmailService
{
    private static readonly CultureInfo VietnamCulture = CultureInfo.GetCultureInfo("vi-VN");

    private readonly ApplicationDbContext _context;
    private readonly IEmailSender _emailSender;
    private readonly ILogger<BookingEmailService> _logger;

    public BookingEmailService(
        ApplicationDbContext context,
        IEmailSender emailSender,
        ILogger<BookingEmailService> logger)
    {
        _context = context;
        _emailSender = emailSender;
        _logger = logger;
    }

    public async Task SendPaymentSuccessEmailAsync(
        PaymentCallbackResult paymentResult,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var invoice = await _context.HoaDons
                .AsNoTracking()
                .Include(x => x.MaDatPhongNavigation)
                .ThenInclude(x => x.MaKhNavigation)
                .Include(x => x.ChiTietHoaDons)
                .FirstOrDefaultAsync(x => x.MaDatPhong == paymentResult.BookingCode, cancellationToken);
            if (invoice == null)
            {
                _logger.LogWarning(
                    "Cannot send payment success email because invoice was not found. BookingCode={BookingCode}",
                    paymentResult.BookingCode);
                return;
            }

            var booking = invoice.MaDatPhongNavigation;
            var customerEmail = booking.MaKhNavigation.Email?.Trim();
            if (string.IsNullOrWhiteSpace(customerEmail))
            {
                _logger.LogWarning(
                    "Cannot send payment success email because customer email is missing. BookingCode={BookingCode}",
                    paymentResult.BookingCode);
                return;
            }

            var subject = $"Xac nhan thanh toan thanh cong - {booking.MaDatPhong}";
            var textBody = BuildTextBody(invoice, paymentResult);
            var htmlBody = BuildHtmlBody(invoice, paymentResult);

            await _emailSender.SendAsync(
                customerEmail,
                subject,
                htmlBody,
                textBody,
                cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Failed to send payment success email. BookingCode={BookingCode}",
                paymentResult.BookingCode);
        }
    }

    private static string BuildTextBody(HoaDon invoice, PaymentCallbackResult paymentResult)
    {
        var booking = invoice.MaDatPhongNavigation;
        var roomLines = GetRoomLines(invoice).ToList();
        var rooms = roomLines.Count == 0
            ? "- Thong tin phong dang duoc cap nhat"
            : string.Join(Environment.NewLine, roomLines.Select(line =>
                $"- {line.NoiDung} | So dem: {line.SoLuong} | Don gia: {FormatMoney(line.DonGia)} | Thanh tien: {FormatMoney(line.ThanhTien)}"));

        return $"""
        Xin chao {booking.TenKhSnapshot},

        Venus Hotel xac nhan thanh toan VNPay cua quy khach da thanh cong.

        Ma dat phong: {booking.MaDatPhong}
        Ngay nhan phong: {booking.NgayNhanPhong:dd/MM/yyyy}
        Ngay tra phong: {booking.NgayTraPhong:dd/MM/yyyy}
        Tong tien da thanh toan: {FormatMoney(invoice.SoTienDaThanhToan)}
        Ma giao dich VNPay: {paymentResult.TransactionNo ?? "N/A"}
        Ngan hang: {paymentResult.BankCode ?? "N/A"}
        Thoi gian thanh toan: {FormatDateTime(invoice.NgayThanhToanCuoi)}

        Thong tin phong:
        {rooms}

        Cam on quy khach da dat phong tai Venus Hotel.
        """;
    }

    private static string BuildHtmlBody(HoaDon invoice, PaymentCallbackResult paymentResult)
    {
        var booking = invoice.MaDatPhongNavigation;
        var roomRows = GetRoomLines(invoice).Select(line => $"""
            <tr>
                <td>{Html(line.NoiDung)}</td>
                <td style="text-align:right">{line.SoLuong}</td>
                <td style="text-align:right">{Html(FormatMoney(line.DonGia))}</td>
                <td style="text-align:right">{Html(FormatMoney(line.ThanhTien))}</td>
            </tr>
            """);
        var roomsHtml = string.Join(Environment.NewLine, roomRows);
        if (string.IsNullOrWhiteSpace(roomsHtml))
        {
            roomsHtml = """
            <tr>
                <td colspan="4">Thong tin phong dang duoc cap nhat</td>
            </tr>
            """;
        }

        return $"""
        <!doctype html>
        <html>
        <body style="font-family:Arial,sans-serif;color:#1f2937;line-height:1.5">
            <h2>Thanh toan VNPay thanh cong</h2>
            <p>Xin chao {Html(booking.TenKhSnapshot)},</p>
            <p>Venus Hotel xac nhan thanh toan VNPay cua quy khach da thanh cong.</p>

            <table style="border-collapse:collapse;margin:16px 0">
                <tr><td><strong>Ma dat phong</strong></td><td style="padding-left:16px">{Html(booking.MaDatPhong)}</td></tr>
                <tr><td><strong>Ngay nhan phong</strong></td><td style="padding-left:16px">{booking.NgayNhanPhong:dd/MM/yyyy}</td></tr>
                <tr><td><strong>Ngay tra phong</strong></td><td style="padding-left:16px">{booking.NgayTraPhong:dd/MM/yyyy}</td></tr>
                <tr><td><strong>Tong tien da thanh toan</strong></td><td style="padding-left:16px">{Html(FormatMoney(invoice.SoTienDaThanhToan))}</td></tr>
                <tr><td><strong>Ma giao dich VNPay</strong></td><td style="padding-left:16px">{Html(paymentResult.TransactionNo ?? "N/A")}</td></tr>
                <tr><td><strong>Ngan hang</strong></td><td style="padding-left:16px">{Html(paymentResult.BankCode ?? "N/A")}</td></tr>
                <tr><td><strong>Thoi gian thanh toan</strong></td><td style="padding-left:16px">{Html(FormatDateTime(invoice.NgayThanhToanCuoi))}</td></tr>
            </table>

            <h3>Thong tin phong</h3>
            <table style="border-collapse:collapse;width:100%" border="1" cellpadding="8">
                <thead>
                    <tr>
                        <th style="text-align:left">Noi dung</th>
                        <th style="text-align:right">So dem</th>
                        <th style="text-align:right">Don gia</th>
                        <th style="text-align:right">Thanh tien</th>
                    </tr>
                </thead>
                <tbody>
                    {roomsHtml}
                </tbody>
            </table>

            <p>Cam on quy khach da dat phong tai Venus Hotel.</p>
        </body>
        </html>
        """;
    }

    private static IEnumerable<ChiTietHoaDon> GetRoomLines(HoaDon invoice)
    {
        return invoice.ChiTietHoaDons
            .Where(x => x.LoaiMuc == DomainValues.ChiTietHoaDonLoaiMuc.Phong)
            .OrderBy(x => x.MaCthd);
    }

    private static string FormatMoney(decimal value)
    {
        return string.Format(VietnamCulture, "{0:N0} VND", value);
    }

    private static string FormatDateTime(DateTime? value)
    {
        if (!value.HasValue)
        {
            return "N/A";
        }

        var utcValue = value.Value.Kind == DateTimeKind.Utc
            ? value.Value
            : DateTime.SpecifyKind(value.Value, DateTimeKind.Utc);
        var vietnamTime = TimeZoneInfo.ConvertTimeFromUtc(utcValue, GetVietnamTimeZone());
        return vietnamTime.ToString("dd/MM/yyyy HH:mm", VietnamCulture);
    }

    private static string Html(string value)
    {
        return WebUtility.HtmlEncode(value);
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
}
