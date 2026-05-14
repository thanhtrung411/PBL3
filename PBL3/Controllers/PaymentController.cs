using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PBL3.Data;
using PBL3.Models;
using PBL3.Services.Interfaces;

namespace PBL3.Controllers;

[AllowAnonymous]
public class PaymentController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IVnPayService _vnPayService;

    public PaymentController(ApplicationDbContext context, IVnPayService vnPayService)
    {
        _context = context;
        _vnPayService = vnPayService;
    }

    [HttpGet]
    public async Task<IActionResult> Start(string? id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            TempData["Error"] = "Không tìm thấy mã đặt phòng để thanh toán.";
            return RedirectToAction("Index", "Booking");
        }

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

        if (invoice.TrangThai == DomainValues.HoaDonTrangThai.DaThanhToan)
        {
            TempData["Success"] = "Đặt phòng này đã được thanh toán.";
            return RedirectToAction("Success", "Booking", new { id = bookingCode });
        }

        var returnUrl = Url.Action(nameof(VnPayReturn), "Payment", null, Request.Scheme, Request.Host.Value) ?? "";
        var result = _vnPayService.CreatePaymentUrl(new VnPayPaymentRequest
        {
            BookingCode = bookingCode,
            Amount = invoice.TongThanhToan,
            OrderInfo = $"Thanh toán đặt phòng {bookingCode}",
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
        return View("Result", result);
    }

    [HttpGet]
    public async Task<IActionResult> VnPayIpn()
    {
        var result = await _vnPayService.ProcessCallbackAsync(Request.Query);
        if (!result.IsValidSignature)
        {
            return Json(new { RspCode = "97", Message = "Invalid signature" });
        }

        if (string.IsNullOrWhiteSpace(result.BookingCode))
        {
            return Json(new { RspCode = "01", Message = "Order not found" });
        }

        if (!result.Success)
        {
            return Json(new { RspCode = "00", Message = "Confirm success" });
        }

        return Json(new { RspCode = "00", Message = "Confirm success" });
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
}
