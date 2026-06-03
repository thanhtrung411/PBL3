using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using PBL3.Data;
using PBL3.Models;
using PBL3.Services.Interfaces;

namespace PBL3.Controllers;

[AllowAnonymous]
public class PaymentController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IVnPayService _vnPayService;
    private readonly IExpiredBookingCleanupService _expiredBookingCleanupService;
    private readonly VnPayOptions _vnPayOptions;

    public PaymentController(
        ApplicationDbContext context,
        IVnPayService vnPayService,
        IExpiredBookingCleanupService expiredBookingCleanupService,
        IOptions<VnPayOptions> vnPayOptions)
    {
        _context = context;
        _vnPayService = vnPayService;
        _expiredBookingCleanupService = expiredBookingCleanupService;
        _vnPayOptions = vnPayOptions.Value;
    }

    [HttpGet]
    public async Task<IActionResult> Start(string? id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            TempData["Error"] = "Không tìm thấy mã đặt phòng để thanh toán.";
            return RedirectToAction("Index", "Booking");
        }

        await _expiredBookingCleanupService.CancelExpiredOnlinePaymentsAsync();

        var bookingCode = id.Trim();
        var invoice = await _context.HoaDons
            .AsNoTracking()
            .Include(x => x.MaDatPhongNavigation)
            .FirstOrDefaultAsync(x => x.MaDatPhong == bookingCode);
        if (invoice == null)
        {
            TempData["Error"] = "Không tìm thấy hóa đơn thanh toán.";
            return RedirectToAction("Success", "Booking", new { id = bookingCode });
        }

        if (invoice.TrangThai == DomainValues.HoaDonTrangThai.DaHuy ||
            invoice.MaDatPhongNavigation.TrangThai == DomainValues.DatPhongTrangThai.DaHuy)
        {
            TempData["Error"] = "Đơn đặt phòng đã quá hạn thanh toán và đã được hủy. Vui lòng đặt phòng lại.";
            return RedirectToAction("Index", "Booking");
        }

        if (invoice.TrangThai == DomainValues.HoaDonTrangThai.DaThanhToan)
        {
            TempData["Success"] = "Đặt phòng này đã được thanh toán.";
            return RedirectToAction("Success", "Booking", new { id = bookingCode });
        }

        var returnUrl = ResolveVnPayReturnUrl();
        var result = _vnPayService.CreatePaymentUrl(new VnPayPaymentRequest
        {
            BookingCode = bookingCode,
            Amount = invoice.TongThanhToan,
            OrderInfo = $"Thanh toan dat phong {bookingCode}",
            IpAddress = GetClientIpAddress(),
            ReturnUrl = returnUrl
        });

        if (!result.Success || string.IsNullOrWhiteSpace(result.PaymentUrl))
        {
            TempData["Error"] = result.ErrorMessage ?? "VNPay chưa sẵn sàng. Đặt phòng vẫn được giữ chỗ và bạn có thể thanh toán tại khách sạn.";
            return RedirectToAction("Success", "Booking", new { id = bookingCode });
        }

        return Redirect(result.PaymentUrl);
    }

    [HttpGet]
    public async Task<IActionResult> VnPayReturn()
    {
        var result = await _vnPayService.ProcessCallbackAsync(Request.Query);
        if (result.IsWalkInPayment)
        {
            var status = result.Success
                ? "success"
                : string.Equals(result.ResponseCode, "24", StringComparison.OrdinalIgnoreCase)
                    ? "cancelled"
                    : "failed";

            return RedirectToAction(
                "Index",
                "Receptionist",
                new
                {
                    walkInPaymentStatus = status,
                    bookingCode = result.BookingCode,
                    message = result.Message
                });
        }

        if (result.IsCheckoutPayment)
        {
            var status = result.Success
                ? "success"
                : string.Equals(result.ResponseCode, "24", StringComparison.OrdinalIgnoreCase)
                    ? "cancelled"
                    : "failed";

            return RedirectToAction(
                "Index",
                "Receptionist",
                new
                {
                    checkoutPaymentStatus = status,
                    bookingCode = result.BookingCode,
                    amount = result.Amount,
                    transactionNo = result.TransactionNo,
                    bankCode = result.BankCode,
                    message = result.Message,
                    printInvoice = Request.Query["printInvoice"].ToString(),
                    sendEmail = Request.Query["sendEmail"].ToString(),
                    receiptEmail = Request.Query["receiptEmail"].ToString()
                });
        }

        return View("Result", result);
    }

    [HttpGet]
    public async Task<IActionResult> VnPayIpn()
    {
        var result = await _vnPayService.ProcessCallbackAsync(Request.Query);
        return Json(new { RspCode = result.IpnResponseCode, Message = result.IpnMessage });
    }

    private string GetClientIpAddress()
    {
        var forwarded = Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(forwarded))
        {
            return forwarded.Split(',')[0].Trim();
        }

        return HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";
    }

    private string ResolveVnPayReturnUrl()
    {
        var currentHostUrl = Url.Action(nameof(VnPayReturn), "Payment", null, Request.Scheme, Request.Host.Value) ?? "";
        var configuredUrl = _vnPayOptions.ReturnUrl?.Trim();
        if (string.IsNullOrWhiteSpace(configuredUrl))
        {
            return currentHostUrl;
        }

        if (IsLocalHost(Request.Host.Host) && !string.IsNullOrWhiteSpace(currentHostUrl))
        {
            return currentHostUrl;
        }

        if (Uri.TryCreate(configuredUrl, UriKind.Absolute, out var configuredUri))
        {
            var builder = new UriBuilder(configuredUri)
            {
                Path = NormalizeReturnPath(configuredUri.AbsolutePath)
            };

            return builder.Uri.ToString();
        }

        return configuredUrl.Replace("//Payment/", "/Payment/", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsLocalHost(string? host)
    {
        return string.Equals(host, "localhost", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(host, "127.0.0.1", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(host, "::1", StringComparison.OrdinalIgnoreCase);
    }

    private static string NormalizeReturnPath(string path)
    {
        if (string.IsNullOrWhiteSpace(path) || path == "/")
        {
            return "/Payment/VnPayReturn";
        }

        while (path.Contains("//", StringComparison.Ordinal))
        {
            path = path.Replace("//", "/", StringComparison.Ordinal);
        }

        return path;
    }
}
