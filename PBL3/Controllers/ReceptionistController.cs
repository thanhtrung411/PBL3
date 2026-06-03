using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PBL3.Models;
using PBL3.Services.Interfaces;
using PBL3.Services.Receptionist;
using System.Security.Claims;

namespace PBL3.Controllers;

[Authorize]
public class ReceptionistController : Controller
{
    private readonly IReceptionistCheckInService _receptionistCheckInService;
    private readonly IVnPayService _vnPayService;

    public ReceptionistController(
        IReceptionistCheckInService receptionistCheckInService,
        IVnPayService vnPayService)
    {
        _receptionistCheckInService = receptionistCheckInService;
        _vnPayService = vnPayService;
    }

    [HttpGet]
    public IActionResult Index()
    {
        return View("~/Views/Test/Receptionist.cshtml");
    }

    [HttpGet]
    public async Task<IActionResult> Today(CancellationToken cancellationToken)
    {
        var result = await _receptionistCheckInService.GetTodayArrivalsAsync(cancellationToken);
        return Ok(result);
    }

    [HttpGet]
    public async Task<IActionResult> RoomMap(CancellationToken cancellationToken)
    {
        var result = await _receptionistCheckInService.GetRoomMapAsync(cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> RoomMaintenance(
        [FromBody] RoomMaintenanceRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _receptionistCheckInService.SetRoomMaintenanceAsync(
            request ?? new RoomMaintenanceRequest(),
            cancellationToken);
        return Ok(result);
    }

    [HttpGet]
    public async Task<IActionResult> ServiceUsage(CancellationToken cancellationToken)
    {
        var result = await _receptionistCheckInService.GetServiceUsageAsync(cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> ServiceUsage(
        [FromBody] ReceptionistAddServiceRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _receptionistCheckInService.AddServiceUsageAsync(
            request ?? new ReceptionistAddServiceRequest(),
            cancellationToken);
        return Ok(result);
    }

    [HttpGet]
    public async Task<IActionResult> Checkout(CancellationToken cancellationToken)
    {
        var result = await _receptionistCheckInService.GetCheckoutListAsync(cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> Checkout(
        [FromBody] ReceptionistCheckoutRequest request,
        CancellationToken cancellationToken)
    {
        request ??= new ReceptionistCheckoutRequest();
        if (IsVnPayPayment(request.PaymentMethod))
        {
            var bookingCode = (request.BookingCode ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(bookingCode))
            {
                return Ok(new ReceptionistCheckoutResult
                {
                    Success = false,
                    Message = "Thiếu mã đặt phòng."
                });
            }

            var checkoutList = await _receptionistCheckInService.GetCheckoutListAsync(cancellationToken);
            var stay = checkoutList.ActiveStays.FirstOrDefault(x =>
                string.Equals(x.BookingCode, bookingCode, StringComparison.OrdinalIgnoreCase));
            if (stay == null)
            {
                return Ok(new ReceptionistCheckoutResult
                {
                    Success = false,
                    BookingCode = bookingCode,
                    Message = "Không tìm thấy khách đang lưu trú để thanh toán VNPay."
                });
            }

            if (stay.RemainingAmount <= 0)
            {
                return Ok(new ReceptionistCheckoutResult
                {
                    Success = false,
                    BookingCode = stay.BookingCode,
                    Message = "Hóa đơn đã thu đủ, không cần thanh toán VNPay."
                });
            }

            var paymentStart = _vnPayService.CreatePaymentUrl(new VnPayPaymentRequest
            {
                BookingCode = stay.BookingCode,
                Amount = stay.RemainingAmount,
                OrderInfo = $"CHECKOUT_VNPAY_PENDING Thanh toan check-out {stay.BookingCode}",
                IpAddress = GetClientIpAddress(),
                ReturnUrl = ResolveCurrentHostVnPayReturnUrl()
            });

            if (!paymentStart.Success || string.IsNullOrWhiteSpace(paymentStart.PaymentUrl))
            {
                return Ok(new ReceptionistCheckoutResult
                {
                    Success = false,
                    BookingCode = stay.BookingCode,
                    PaidAmount = stay.PaidAmount,
                    GrandTotal = stay.GrandTotal,
                    RemainingAmount = stay.RemainingAmount,
                    Message = paymentStart.ErrorMessage ?? "Không tạo được thanh toán VNPay."
                });
            }

            return Ok(new ReceptionistCheckoutResult
            {
                Success = true,
                BookingCode = stay.BookingCode,
                PaidAmount = stay.PaidAmount,
                GrandTotal = stay.GrandTotal,
                RemainingAmount = stay.RemainingAmount,
                RequiresOnlinePayment = true,
                PaymentUrl = paymentStart.PaymentUrl,
                Message = "Đang chuyển sang VNPay để thanh toán."
            });
        }

        var result = await _receptionistCheckInService.CheckoutAsync(
            request,
            cancellationToken);
        return Ok(result);
    }

    [HttpGet]
    public async Task<IActionResult> Lookup(string code, CancellationToken cancellationToken)
    {
        var result = await _receptionistCheckInService.LookupBookingAsync(code, cancellationToken);
        return Ok(result);
    }

    [HttpGet]
    public async Task<IActionResult> Rooms(string bookingCode, CancellationToken cancellationToken)
    {
        var result = await _receptionistCheckInService.GetRoomSelectionAsync(bookingCode, cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> CheckIn(
        [FromBody] ReceptionistCheckInRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _receptionistCheckInService.CheckInAsync(
            request ?? new ReceptionistCheckInRequest(),
            cancellationToken);
        return Ok(result);
    }

    [HttpGet]
    public async Task<IActionResult> WalkInAvailability(
        DateOnly checkInDate,
        DateOnly checkOutDate,
        CancellationToken cancellationToken)
    {
        var result = await _receptionistCheckInService.GetWalkInAvailabilityAsync(
            new ReceptionistWalkInAvailabilityRequest
            {
                CheckInDate = checkInDate,
                CheckOutDate = checkOutDate
            },
            cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> WalkInCheckIn(
        [FromBody] ReceptionistWalkInCheckInRequest request,
        CancellationToken cancellationToken)
    {
        request ??= new ReceptionistWalkInCheckInRequest();
        request.EmployeeId = User.FindFirstValue("EmployeeId");
        var result = await _receptionistCheckInService.WalkInCheckInAsync(request, cancellationToken);
        if (result.Success && result.RequiresOnlinePayment)
        {
            var paymentStart = _vnPayService.CreatePaymentUrl(new VnPayPaymentRequest
            {
                BookingCode = result.BookingCode,
                Amount = result.GrandTotal,
                OrderInfo = $"WALKIN_VNPAY_PENDING Thanh toan check-in vang lai {result.BookingCode}",
                IpAddress = GetClientIpAddress(),
                ReturnUrl = ResolveCurrentHostVnPayReturnUrl()
            });

            if (!paymentStart.Success || string.IsNullOrWhiteSpace(paymentStart.PaymentUrl))
            {
                await _receptionistCheckInService.CancelWalkInPendingPaymentAsync(result.BookingCode, cancellationToken);
                result.Success = false;
                result.RequiresOnlinePayment = false;
                result.Message = paymentStart.ErrorMessage ?? "Không tạo được thanh toán VNPay. Các phòng đã chọn đã được hủy giữ.";
            }
            else
            {
                result.PaymentUrl = paymentStart.PaymentUrl;
            }
        }

        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> WalkInPromotionPreview(
        [FromBody] ReceptionistWalkInPromotionPreviewRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _receptionistCheckInService.PreviewWalkInPromotionAsync(
            request ?? new ReceptionistWalkInPromotionPreviewRequest(),
            cancellationToken);
        return Ok(result);
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

    private string ResolveCurrentHostVnPayReturnUrl()
    {
        return Url.Action("VnPayReturn", "Payment", null, Request.Scheme, Request.Host.Value) ?? "/Payment/VnPayReturn";
    }

    private static bool IsVnPayPayment(string? paymentMethod)
    {
        return string.Equals(paymentMethod?.Trim(), "VNPAY", StringComparison.OrdinalIgnoreCase);
    }
}
