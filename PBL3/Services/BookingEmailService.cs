using System.Globalization;
using System.Net;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using PBL3.Data;
using PBL3.Models;
using PBL3.Services.Interfaces;
using QRCoder;

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

            var qrPayload = BuildQrPayload(booking.MaDatPhong);
            var qrImage = GenerateQrPng(qrPayload);
            var subject = $"Hóa đơn thanh toán thành công - {booking.MaDatPhong}";
            var textBody = BuildTextBody(invoice, paymentResult, qrPayload);
            var htmlBody = BuildHtmlBody(invoice, paymentResult);

            await _emailSender.SendAsync(
                customerEmail,
                subject,
                htmlBody,
                textBody,
                new[] { new EmailInlineImage("booking-qr", "image/png", qrImage) },
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

    public async Task<bool> SendCheckoutReceiptEmailAsync(
        string bookingCode,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var invoice = await _context.HoaDons
                .AsNoTracking()
                .Include(x => x.MaDatPhongNavigation)
                .ThenInclude(x => x.MaKhNavigation)
                .Include(x => x.ChiTietHoaDons)
                .ThenInclude(x => x.MaDvNavigation)
                .Include(x => x.ChiTietHoaDons)
                .ThenInclude(x => x.MaLoaiPhongNavigation)
                .Include(x => x.ChiTietHoaDons)
                .ThenInclude(x => x.MaPhongNavigation)
                .FirstOrDefaultAsync(x => x.MaDatPhong == bookingCode, cancellationToken);
            if (invoice == null)
            {
                _logger.LogWarning(
                    "Cannot send checkout receipt email because invoice was not found. BookingCode={BookingCode}",
                    bookingCode);
                return false;
            }

            var booking = invoice.MaDatPhongNavigation;
            var customerEmail = booking.MaKhNavigation.Email?.Trim();
            if (string.IsNullOrWhiteSpace(customerEmail))
            {
                _logger.LogWarning(
                    "Cannot send checkout receipt email because customer email is missing. BookingCode={BookingCode}",
                    bookingCode);
                return false;
            }

            var subject = $"Hóa đơn check-out - {booking.MaDatPhong}";
            var textBody = BuildCheckoutTextBody(invoice);
            var htmlBody = BuildCheckoutHtmlBody(invoice);

            await _emailSender.SendAsync(
                customerEmail,
                subject,
                htmlBody,
                textBody,
                null,
                cancellationToken);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Failed to send checkout receipt email. BookingCode={BookingCode}",
                bookingCode);
            return false;
        }
    }

    private static string BuildCheckoutTextBody(HoaDon invoice)
    {
        var booking = invoice.MaDatPhongNavigation;
        var invoiceLines = GetInvoiceLines(invoice).ToList();
        var lines = invoiceLines.Count == 0
            ? "- Không có dòng hóa đơn."
            : string.Join(Environment.NewLine, invoiceLines.Select(line =>
                $"- {BuildInvoiceLineName(line)} | SL: {BuildQuantityLabel(line)} | Đơn giá: {FormatMoney(line.DonGia)} | Thành tiền: {FormatMoney(line.ThanhTien)}"));
        var remaining = Math.Max(invoice.TongThanhToan - invoice.SoTienDaThanhToan, 0);

        return $"""
        Xin chào {booking.TenKhSnapshot},

        Venus Hotel xác nhận quý khách đã hoàn tất thủ tục check-out.

        Mã đặt phòng: {booking.MaDatPhong}
        Mã hóa đơn: {invoice.MaHoaDon}
        Họ tên: {booking.TenKhSnapshot}
        Số điện thoại: {booking.SdtSnapshot ?? "N/A"}
        Ngày nhận phòng: {booking.NgayNhanPhong:dd/MM/yyyy}
        Ngày trả phòng: {booking.NgayTraPhong:dd/MM/yyyy}
        Số đêm: {GetNights(booking)}

        Chi tiết hóa đơn:
        {lines}

        Tổng tiền phòng: {FormatMoney(invoice.TongTienPhong)}
        Tổng tiền dịch vụ: {FormatMoney(invoice.TongTienDichVu)}
        Giảm giá: {FormatMoney(invoice.TienGiamGiaPhong)}
        Tổng thanh toán: {FormatMoney(invoice.TongThanhToan)}
        Đã thanh toán: {FormatMoney(invoice.SoTienDaThanhToan)}
        Còn lại: {FormatMoney(remaining)}
        Phương thức thanh toán cuối: {invoice.PhuongThucThanhToan ?? "N/A"}
        Thời gian thanh toán cuối: {FormatDateTime(invoice.NgayThanhToanCuoi)}

        Cảm ơn quý khách đã lưu trú tại Venus Hotel. Hẹn gặp lại quý khách trong những chuyến nghỉ dưỡng tiếp theo.
        """;
    }

    private static string BuildCheckoutHtmlBody(HoaDon invoice)
    {
        var booking = invoice.MaDatPhongNavigation;
        var remaining = Math.Max(invoice.TongThanhToan - invoice.SoTienDaThanhToan, 0);
        var roomRowsHtml = BuildCheckoutRows(
            invoice.ChiTietHoaDons.Where(x => x.LoaiMuc == DomainValues.ChiTietHoaDonLoaiMuc.Phong));
        var serviceRowsHtml = BuildCheckoutRows(
            invoice.ChiTietHoaDons.Where(x => x.LoaiMuc == DomainValues.ChiTietHoaDonLoaiMuc.DichVu));

        return $$"""
        <!doctype html>
        <html>
        <head>
            <meta name="viewport" content="width=device-width, initial-scale=1.0">
            <style>
                @media only screen and (max-width: 640px) {
                    .email-shell { padding: 0 !important; }
                    .email-card { border-radius: 0 !important; }
                    .email-header { border-radius: 0 !important; padding: 24px 18px !important; }
                    .email-header h1 { font-size: 24px !important; }
                    .email-body { padding: 20px 16px !important; }
                    .stack-column { display: block !important; width: 100% !important; padding-right: 0 !important; }
                    .summary-column { display: block !important; width: 100% !important; }
                    .summary-spacer { display: none !important; }
                    .invoice-table th, .invoice-table td { font-size: 12px !important; padding: 9px 6px !important; }
                    .info-label { display: block !important; width: auto !important; margin-bottom: 2px !important; }
                }
            </style>
        </head>
        <body style="margin:0;background:#f4f7fb;font-family:Arial,sans-serif;color:#1f2937;line-height:1.5">
            <div class="email-shell" style="max-width:760px;margin:0 auto;padding:24px">
                <div class="email-header" style="background:#0f766e;border-radius:18px 18px 0 0;padding:28px;color:#ffffff">
                    <div style="font-size:13px;letter-spacing:2px;text-transform:uppercase;color:#f9e8a8;font-weight:700">Venus Hotel</div>
                    <h1 style="margin:8px 0 6px;font-size:28px;line-height:1.2">Hóa đơn check-out</h1>
                    <div style="display:inline-block;color:#ecfeff;font-weight:700;font-size:13px">
                        <span style="display:inline-block;width:18px;height:18px;line-height:18px;text-align:center;background:#14b8a6;color:#ffffff;border-radius:50%;margin-right:7px;font-size:12px">✓</span>
                        <span>Check-out completed</span>
                    </div>
                </div>

                <div class="email-card email-body" style="background:#ffffff;border:1px solid #e5e7eb;border-top:0;border-radius:0 0 18px 18px;padding:26px">
                    <p style="margin:0 0 18px;font-size:16px">Xin chào <strong>{{Html(booking.TenKhSnapshot)}}</strong>, Venus Hotel xác nhận quý khách đã hoàn tất thủ tục check-out. Hóa đơn cuối cùng của kỳ lưu trú được gửi kèm bên dưới.</p>

                    <table style="width:100%;border-collapse:collapse;margin:18px 0">
                        <tr>
                            <td class="stack-column" style="vertical-align:top;width:58%;padding-right:18px">
                                <h2 style="font-size:18px;margin:0 0 10px;color:#111827">Thông tin lưu trú</h2>
                                {{BuildInfoRow("Mã đặt phòng", booking.MaDatPhong)}}
                                {{BuildInfoRow("Mã hóa đơn", invoice.MaHoaDon)}}
                                {{BuildInfoRow("Họ tên", booking.TenKhSnapshot)}}
                                {{BuildInfoRow("Số điện thoại", booking.SdtSnapshot ?? "N/A")}}
                                {{BuildInfoRow("CCCD/CMND", MaskIdentityNumber(booking.CccdSnapshot))}}
                                {{BuildInfoRow("Ngày nhận phòng", booking.NgayNhanPhong.ToString("dd/MM/yyyy", VietnamCulture))}}
                                {{BuildInfoRow("Ngày trả phòng", booking.NgayTraPhong.ToString("dd/MM/yyyy", VietnamCulture))}}
                                {{BuildInfoRow("Số đêm", GetNights(booking).ToString(CultureInfo.InvariantCulture))}}
                            </td>
                            <td class="summary-column" style="vertical-align:top;width:42%;background:#f0fdfa;border:1px solid #99f6e4;border-radius:14px;padding:18px">
                                <div style="font-size:13px;text-transform:uppercase;color:#0f766e;font-weight:800;margin-bottom:10px">Thanh toán</div>
                                <table style="width:100%;border-collapse:collapse">
                                    {{BuildMoneyRow("Tổng hóa đơn", invoice.TongThanhToan, true)}}
                                    {{BuildMoneyRow("Đã thanh toán", invoice.SoTienDaThanhToan, true)}}
                                    {{BuildMoneyRow("Còn lại", remaining, true)}}
                                </table>
                            </td>
                        </tr>
                    </table>

                    <h2 style="font-size:18px;margin:22px 0 10px;color:#111827">Tiền phòng</h2>
                    {{BuildCheckoutTable(roomRowsHtml)}}

                    <h2 style="font-size:18px;margin:22px 0 10px;color:#111827">Dịch vụ sử dụng</h2>
                    {{BuildCheckoutTable(serviceRowsHtml)}}

                    <table style="width:100%;border-collapse:collapse;margin:20px 0 0">
                        <tr>
                            <td class="summary-column" style="width:48%;vertical-align:top;background:#f8fafc;border:1px solid #e2e8f0;border-radius:14px;padding:16px;color:#475569">
                                <strong>Thông tin thanh toán cuối</strong><br />
                                Phương thức: {{Html(invoice.PhuongThucThanhToan ?? "N/A")}}<br />
                                Thời gian: {{Html(FormatDateTime(invoice.NgayThanhToanCuoi))}}
                            </td>
                            <td class="summary-spacer" style="width:4%"></td>
                            <td class="summary-column" style="width:48%;vertical-align:top">
                                <table style="width:100%;border-collapse:collapse">
                                    {{BuildMoneyRow("Tổng tiền phòng", invoice.TongTienPhong)}}
                                    {{BuildMoneyRow("Tổng tiền dịch vụ", invoice.TongTienDichVu)}}
                                    {{BuildMoneyRow("Giảm giá", invoice.TienGiamGiaPhong)}}
                                    {{BuildMoneyRow("Tổng thanh toán", invoice.TongThanhToan, true)}}
                                    {{BuildMoneyRow("Đã thanh toán", invoice.SoTienDaThanhToan, true)}}
                                    {{BuildMoneyRow("Còn lại", remaining, true)}}
                                </table>
                            </td>
                        </tr>
                    </table>

                    <p style="margin:22px 0 0;color:#64748b;font-size:14px">Cảm ơn quý khách đã lưu trú tại Venus Hotel. Hẹn gặp lại quý khách trong những chuyến nghỉ dưỡng tiếp theo.</p>
                </div>
            </div>
        </body>
        </html>
        """;
    }

    private static string BuildCheckoutRows(IEnumerable<ChiTietHoaDon> lines)
    {
        var rows = lines
            .OrderBy(x => x.MaCthd)
            .Select(line => $"""
            <tr>
                <td style="padding:12px 10px;border-bottom:1px solid #edf2f7;color:#111827">{Html(BuildInvoiceLineName(line))}</td>
                <td style="padding:12px 10px;border-bottom:1px solid #edf2f7;text-align:right;color:#111827">{Html(BuildQuantityLabel(line))}</td>
                <td style="padding:12px 10px;border-bottom:1px solid #edf2f7;text-align:right;color:#111827">{Html(FormatMoney(line.DonGia))}</td>
                <td style="padding:12px 10px;border-bottom:1px solid #edf2f7;text-align:right;color:#111827;font-weight:700">{Html(FormatMoney(line.ThanhTien))}</td>
            </tr>
            """)
            .ToList();

        return rows.Count == 0
            ? """
            <tr>
                <td colspan="4" style="padding:12px 10px;border-bottom:1px solid #edf2f7;color:#64748b;text-align:center">Không có phát sinh.</td>
            </tr>
            """
            : string.Join(Environment.NewLine, rows);
    }

    private static string BuildCheckoutTable(string rowsHtml)
    {
        return $$"""
        <table class="invoice-table" style="border-collapse:collapse;width:100%;border:1px solid #e5e7eb;border-radius:12px;overflow:hidden">
            <thead>
                <tr style="background:#f8fafc">
                    <th style="padding:12px 10px;text-align:left;color:#475569;font-size:13px">Nội dung</th>
                    <th style="padding:12px 10px;text-align:right;color:#475569;font-size:13px">Số lượng</th>
                    <th style="padding:12px 10px;text-align:right;color:#475569;font-size:13px">Đơn giá</th>
                    <th style="padding:12px 10px;text-align:right;color:#475569;font-size:13px">Thành tiền</th>
                </tr>
            </thead>
            <tbody>
                {{rowsHtml}}
            </tbody>
        </table>
        """;
    }

    private static string BuildInvoiceLineName(ChiTietHoaDon line)
    {
        if (line.LoaiMuc == DomainValues.ChiTietHoaDonLoaiMuc.Phong)
        {
            var roomType = line.MaLoaiPhongNavigation?.TenLoaiPhong ?? line.NoiDung;
            return line.MaPhongNavigation == null
                ? roomType
                : $"{roomType} - Phòng {line.MaPhongNavigation.SoPhong}";
        }

        return line.MaDvNavigation?.TenDv ?? line.NoiDung;
    }

    private static string BuildQuantityLabel(ChiTietHoaDon line)
    {
        if (line.LoaiMuc == DomainValues.ChiTietHoaDonLoaiMuc.Phong)
        {
            return $"{line.SoLuong} đêm";
        }

        var unit = line.MaDvNavigation?.DonViTinh;
        return string.IsNullOrWhiteSpace(unit)
            ? line.SoLuong.ToString(CultureInfo.InvariantCulture)
            : $"{line.SoLuong} {unit}";
    }

    private static string BuildTextBody(HoaDon invoice, PaymentCallbackResult paymentResult, string qrPayload)
    {
        var booking = invoice.MaDatPhongNavigation;
        var invoiceLines = GetInvoiceLines(invoice).ToList();
        var lines = invoiceLines.Count == 0
            ? "- Thông tin hóa đơn đang được cập nhật"
            : string.Join(Environment.NewLine, invoiceLines.Select(line =>
                $"- {line.NoiDung} | Số đêm/SL: {line.SoLuong} | Đơn giá: {FormatMoney(line.DonGia)} | Thành tiền: {FormatMoney(line.ThanhTien)}"));

        return $"""
        Xin chào {booking.TenKhSnapshot},

        Venus Hotel xác nhận thanh toán VNPay của quý khách đã thành công.

        Mã đặt phòng: {booking.MaDatPhong}
        Mã hóa đơn: {invoice.MaHoaDon}
        Họ tên: {booking.TenKhSnapshot}
        Số điện thoại: {booking.SdtSnapshot ?? "N/A"}
        CCCD/CMND: {MaskIdentityNumber(booking.CccdSnapshot)}
        Ngày nhận phòng: {booking.NgayNhanPhong:dd/MM/yyyy}
        Ngày trả phòng: {booking.NgayTraPhong:dd/MM/yyyy}
        Số đêm: {GetNights(booking)}
        Tổng tiền phòng: {FormatMoney(invoice.TongTienPhong)}
        Tổng tiền dịch vụ: {FormatMoney(invoice.TongTienDichVu)}
        Giảm giá: {FormatMoney(invoice.TienGiamGiaPhong)}
        Tổng thanh toán: {FormatMoney(invoice.TongThanhToan)}
        Tổng tiền đã thanh toán: {FormatMoney(invoice.SoTienDaThanhToan)}
        Mã giao dịch VNPay: {paymentResult.TransactionNo ?? "N/A"}
        Ngân hàng: {paymentResult.BankCode ?? "N/A"}
        Thời gian thanh toán: {FormatDateTime(invoice.NgayThanhToanCuoi)}

        Chi tiết hóa đơn:
        {lines}

        Mã QR check-in chứa dữ liệu: {qrPayload}

        Khi đến khách sạn, quý khách vui lòng mang theo giấy tờ tùy thân như CCCD/CMND/Hộ chiếu để làm thủ tục nhận phòng.

        Cảm ơn quý khách đã đặt phòng tại Venus Hotel.
        """;
    }

    private static string BuildHtmlBody(HoaDon invoice, PaymentCallbackResult paymentResult)
    {
        var booking = invoice.MaDatPhongNavigation;
        var invoiceRows = GetInvoiceLines(invoice).Select(line => $"""
            <tr>
                <td style="padding:12px 10px;border-bottom:1px solid #edf2f7;color:#111827">{Html(line.NoiDung)}</td>
                <td style="padding:12px 10px;border-bottom:1px solid #edf2f7;text-align:right;color:#111827">{line.SoLuong}</td>
                <td style="padding:12px 10px;border-bottom:1px solid #edf2f7;text-align:right;color:#111827">{Html(FormatMoney(line.DonGia))}</td>
                <td style="padding:12px 10px;border-bottom:1px solid #edf2f7;text-align:right;color:#111827;font-weight:700">{Html(FormatMoney(line.ThanhTien))}</td>
            </tr>
            """);
        var invoiceRowsHtml = string.Join(Environment.NewLine, invoiceRows);
        if (string.IsNullOrWhiteSpace(invoiceRowsHtml))
        {
            invoiceRowsHtml = """
            <tr>
                <td colspan="4" style="padding:12px 10px;border-bottom:1px solid #edf2f7">Thông tin hóa đơn đang được cập nhật</td>
            </tr>
            """;
        }

        return $$"""
        <!doctype html>
        <html>
        <head>
            <meta name="viewport" content="width=device-width, initial-scale=1.0">
            <style>
                @media only screen and (max-width: 640px) {
                    .email-shell { padding: 0 !important; }
                    .email-card { border-radius: 0 !important; }
                    .email-header { border-radius: 0 !important; padding: 24px 18px !important; }
                    .email-header h1 { font-size: 24px !important; }
                    .email-body { padding: 20px 16px !important; }
                    .stack-column { display: block !important; width: 100% !important; padding-right: 0 !important; }
                    .qr-column { display: block !important; width: 100% !important; padding-top: 18px !important; }
                    .summary-column { display: block !important; width: 100% !important; }
                    .summary-spacer { display: none !important; }
                    .invoice-table th, .invoice-table td { font-size: 12px !important; padding: 9px 6px !important; }
                    .info-label { display: block !important; width: auto !important; margin-bottom: 2px !important; }
                    .qr-image { width: 150px !important; height: 150px !important; }
                }
            </style>
        </head>
        <body style="margin:0;background:#f4f7fb;font-family:Arial,sans-serif;color:#1f2937;line-height:1.5">
            <div class="email-shell" style="max-width:760px;margin:0 auto;padding:24px">
                <div class="email-header" style="background:#101820;border-radius:18px 18px 0 0;padding:28px;color:#ffffff">
                    <div style="font-size:13px;letter-spacing:2px;text-transform:uppercase;color:#d4af37;font-weight:700">Venus Hotel</div>
                    <h1 style="margin:8px 0 6px;font-size:28px;line-height:1.2">Hóa đơn thanh toán thành công</h1>
                    <div style="display:inline-block;color:#137a3a;font-weight:700;font-size:13px">
                        <span style="display:inline-block;width:18px;height:18px;line-height:18px;text-align:center;background:#16a34a;color:#ffffff;border-radius:50%;margin-right:7px;font-size:12px">✓</span>
                        <span>Already paid</span>
                    </div>
                </div>

                <div class="email-card email-body" style="background:#ffffff;border:1px solid #e5e7eb;border-top:0;border-radius:0 0 18px 18px;padding:26px">
                    <p style="margin:0 0 18px;font-size:16px">Xin chào <strong>{{Html(booking.TenKhSnapshot)}}</strong>, Venus Hotel xác nhận giao dịch VNPay của quý khách đã thành công. Thông tin đặt phòng và hóa đơn nằm bên dưới.</p>

                    <table style="width:100%;border-collapse:collapse;margin:18px 0">
                        <tr>
                            <td class="stack-column" style="vertical-align:top;width:58%;padding-right:18px">
                                <h2 style="font-size:18px;margin:0 0 10px;color:#111827">Thông tin đặt phòng</h2>
                                {{BuildInfoRow("Mã đặt phòng", booking.MaDatPhong)}}
                                {{BuildInfoRow("Mã hóa đơn", invoice.MaHoaDon)}}
                                {{BuildInfoRow("Họ tên", booking.TenKhSnapshot)}}
                                {{BuildInfoRow("Số điện thoại", booking.SdtSnapshot ?? "N/A")}}
                                {{BuildInfoRow("CCCD/CMND", MaskIdentityNumber(booking.CccdSnapshot))}}
                                {{BuildInfoRow("Ngày nhận phòng", booking.NgayNhanPhong.ToString("dd/MM/yyyy", VietnamCulture))}}
                                {{BuildInfoRow("Ngày trả phòng", booking.NgayTraPhong.ToString("dd/MM/yyyy", VietnamCulture))}}
                                {{BuildInfoRow("Số đêm", GetNights(booking).ToString(CultureInfo.InvariantCulture))}}
                            </td>
                            <td class="qr-column" style="vertical-align:top;width:42%;text-align:center;background:#f8fafc;border:1px solid #e5e7eb;border-radius:14px;padding:18px">
                                <div style="font-size:13px;text-transform:uppercase;color:#64748b;font-weight:700;margin-bottom:10px">Mã QR check-in</div>
                                <img class="qr-image" src="cid:booking-qr" width="180" height="180" alt="QR đặt phòng {{Html(booking.MaDatPhong)}}" style="display:block;margin:0 auto 10px;border:8px solid #ffffff;border-radius:12px" />
                                <div style="font-size:13px;color:#64748b">Quét mã này tại quầy lễ tân để đối chiếu đặt phòng.</div>
                            </td>
                        </tr>
                    </table>

                    <h2 style="font-size:18px;margin:22px 0 10px;color:#111827">Chi tiết hóa đơn</h2>
                    <table class="invoice-table" style="border-collapse:collapse;width:100%;border:1px solid #e5e7eb;border-radius:12px;overflow:hidden">
                        <thead>
                            <tr style="background:#f8fafc">
                                <th style="padding:12px 10px;text-align:left;color:#475569;font-size:13px">Nội dung</th>
                                <th style="padding:12px 10px;text-align:right;color:#475569;font-size:13px">Số đêm/SL</th>
                                <th style="padding:12px 10px;text-align:right;color:#475569;font-size:13px">Đơn giá</th>
                                <th style="padding:12px 10px;text-align:right;color:#475569;font-size:13px">Thành tiền</th>
                            </tr>
                        </thead>
                        <tbody>
                            {{invoiceRowsHtml}}
                        </tbody>
                    </table>

                    <table style="width:100%;border-collapse:collapse;margin:18px 0 0">
                        <tr>
                            <td class="summary-column" style="width:48%;vertical-align:top;background:#fff7ed;border:1px solid #fed7aa;border-radius:14px;padding:16px;color:#7c2d12">
                                <strong>Lưu ý khi nhận phòng</strong><br />
                                Khi đến khách sạn, quý khách vui lòng mang theo giấy tờ tùy thân như CCCD/CMND/Hộ chiếu để làm thủ tục nhận phòng.
                            </td>
                            <td class="summary-spacer" style="width:4%"></td>
                            <td class="summary-column" style="width:48%;vertical-align:top">
                                <table style="width:100%;border-collapse:collapse">
                                    {{BuildMoneyRow("Tổng tiền phòng", invoice.TongTienPhong)}}
                                    {{BuildMoneyRow("Tổng tiền dịch vụ", invoice.TongTienDichVu)}}
                                    {{BuildMoneyRow("Giảm giá", invoice.TienGiamGiaPhong)}}
                                    {{BuildMoneyRow("Tổng thanh toán", invoice.TongThanhToan, true)}}
                                    {{BuildMoneyRow("Đã thanh toán", invoice.SoTienDaThanhToan, true)}}
                                </table>
                            </td>
                        </tr>
                    </table>

                    <h2 style="font-size:18px;margin:22px 0 10px;color:#111827">Thông tin giao dịch</h2>
                    {{BuildInfoRow("Phương thức", "VNPay")}}
                    {{BuildInfoRow("Mã giao dịch VNPay", paymentResult.TransactionNo ?? "N/A")}}
                    {{BuildInfoRow("Ngân hàng", paymentResult.BankCode ?? "N/A")}}
                    {{BuildInfoRow("Thời gian thanh toán", FormatDateTime(invoice.NgayThanhToanCuoi))}}

                    <p style="margin:22px 0 0;color:#64748b;font-size:14px">Cảm ơn quý khách đã đặt phòng tại Venus Hotel.</p>
                </div>
            </div>
        </body>
        </html>
        """;
    }

    private static string BuildInfoRow(string label, string value)
    {
        return $"""
        <div style="border-bottom:1px solid #edf2f7;padding:8px 0">
            <span class="info-label" style="display:inline-block;width:145px;color:#64748b">{Html(label)}</span>
            <strong style="color:#111827">{Html(value)}</strong>
        </div>
        """;
    }

    private static string BuildMoneyRow(string label, decimal value, bool emphasized = false)
    {
        var fontSize = emphasized ? "18px" : "14px";
        var weight = emphasized ? "800" : "600";
        return $"""
        <tr>
            <td style="padding:7px 0;color:#64748b">{Html(label)}</td>
            <td style="padding:7px 0;text-align:right;font-size:{fontSize};font-weight:{weight};color:#111827">{Html(FormatMoney(value))}</td>
        </tr>
        """;
    }

    private static IEnumerable<ChiTietHoaDon> GetInvoiceLines(HoaDon invoice)
    {
        return invoice.ChiTietHoaDons
            .OrderBy(x => x.LoaiMuc)
            .ThenBy(x => x.MaCthd);
    }

    private static string BuildQrPayload(string bookingCode)
    {
        return JsonSerializer.Serialize(new { booking = bookingCode.Trim() });
    }

    private static byte[] GenerateQrPng(string payload)
    {
        using var generator = new QRCodeGenerator();
        using var qrData = generator.CreateQrCode(payload, QRCodeGenerator.ECCLevel.Q);
        var qrCode = new PngByteQRCode(qrData);
        return qrCode.GetGraphic(10);
    }

    private static int GetNights(DatPhong booking)
    {
        return Math.Max(booking.NgayTraPhong.DayNumber - booking.NgayNhanPhong.DayNumber, 1);
    }

    private static string MaskIdentityNumber(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "N/A";
        }

        var trimmed = value.Trim();
        if (trimmed.Length <= 4)
        {
            return trimmed;
        }

        return new string('*', trimmed.Length - 4) + trimmed[^4..];
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
