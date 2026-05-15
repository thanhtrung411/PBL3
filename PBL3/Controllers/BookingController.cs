using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using PBL3.Models;
using PBL3.Services.Interfaces;
using System.Data.Common;

namespace PBL3.Controllers
{
    [AllowAnonymous]
    public class BookingController : Controller
    {
        private readonly IPublicBookingService _publicBookingService;
        private readonly VnPayOptions _vnPayOptions;

        public BookingController(
            IPublicBookingService publicBookingService,
            IOptions<VnPayOptions> vnPayOptions)
        {
            _publicBookingService = publicBookingService;
            _vnPayOptions = vnPayOptions.Value;
        }

        public async Task<IActionResult> Index()
        {
            var model = await _publicBookingService.SearchRoomsAsync(
                DateTime.Today,
                DateTime.Today.AddDays(1),
                2,
                null);

            return View(model);
        }

        public async Task<IActionResult> Rooms(DateTime? checkIn, DateTime? checkOut, int? guests, string? roomType, int? rooms)
        {
            var model = await _publicBookingService.SearchRoomsAsync(checkIn, checkOut, guests, roomType, rooms);
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Checkout(string? roomId, DateTime? checkIn, DateTime? checkOut, int? guests, int? rooms, string? roomSelection)
        {
            if (string.IsNullOrWhiteSpace(roomId) && string.IsNullOrWhiteSpace(roomSelection))
            {
                TempData["Error"] = "Vui lòng chọn loại phòng trước khi xác nhận đặt phòng.";
                return RedirectToAction(nameof(Rooms), ToRoomsRoute(checkIn, checkOut, guests, null, rooms));
            }

            var normalizedRoomId = roomId?.Trim();
            var model = await _publicBookingService.BuildCheckoutAsync(normalizedRoomId, checkIn, checkOut, guests, rooms, roomSelection);
            if (model == null)
            {
                TempData["Error"] = "Loại phòng này hiện không còn phù hợp với lựa chọn của bạn. Vui lòng chọn lại.";
                return RedirectToAction(nameof(Rooms), ToRoomsRoute(checkIn, checkOut, guests, normalizedRoomId, rooms));
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProcessPayment(CheckoutViewModel data)
        {
            if (!ModelState.IsValid)
            {
                var rebuilt = await RebuildCheckoutModelAsync(data);
                TempData["Error"] = "Vui lòng kiểm tra và điền đầy đủ thông tin bắt buộc.";
                return View("Checkout", rebuilt);
            }

            PublicBookingResult result;
            try
            {
                if (data.PaymentMethod == PaymentMethods.VnPay && !IsVnPayConfigured())
                {
                    data.PaymentMethod = PaymentMethods.PayAtHotel;
                    TempData["Success"] = "VNPay chÆ°a Ä‘Æ°á»£c cáº¥u hÃ¬nh merchant. Há»‡ thá»‘ng Ä‘Ã£ chuyá»ƒn sang giá»¯ chá»— vÃ  thanh toÃ¡n táº¡i khÃ¡ch sáº¡n.";
                }

                result = await _publicBookingService.ConfirmBookingAsync(data);
            }
            catch (Exception ex) when (IsDatabaseException(ex))
            {
                result = new PublicBookingResult
                {
                    Success = false,
                    ErrorMessage = "Không thể kết nối cơ sở dữ liệu để giữ phòng. Vui lòng thử lại sau."
                };
            }

            if (!result.Success)
            {
                TempData["Error"] = result.ErrorMessage ?? "Không thể giữ phòng. Vui lòng thử lại.";
                return RedirectToAction(nameof(Rooms), new
                {
                    checkIn = data.CheckIn.ToString("yyyy-MM-dd"),
                    checkOut = data.CheckOut.ToString("yyyy-MM-dd"),
                    guests = data.Guests,
                    rooms = data.NumberOfRooms,
                    roomType = data.RoomId?.Trim(),
                    roomSelection = data.RoomSelection
                });
            }

            if (data.PaymentMethod == PaymentMethods.VnPay)
            {
                return RedirectToAction("Start", "Payment", new { id = result.BookingCode });
            }

            return RedirectToAction(nameof(Success), new { id = result.BookingCode });
        }

        public IActionResult Success(string? id)
        {
            ViewBag.MaDatPhong = id;
            ViewBag.VnPayAvailable = IsVnPayConfigured();
            ViewBag.PaymentUnavailableMessage = IsVnPayConfigured()
                ? null
                : "VNPay chÆ°a Ä‘Æ°á»£c cáº¥u hÃ¬nh merchant. Äáº·t phÃ²ng váº«n Ä‘Æ°á»£c giá»¯ chá»— vÃ  báº¡n cÃ³ thá»ƒ thanh toÃ¡n táº¡i khÃ¡ch sáº¡n.";
            return View();
        }

        [HttpGet]
        public IActionResult Lookup()
        {
            return View(new BookingLookupViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Lookup(BookingLookupViewModel model)
        {
            model.HasSearched = true;
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                model.Result = await _publicBookingService.LookupAsync(model.BookingCode, model.PhoneNumber);
            }
            catch (Exception ex) when (IsDatabaseException(ex))
            {
                ModelState.AddModelError(string.Empty, "Không thể kết nối cơ sở dữ liệu để tra cứu đặt phòng. Vui lòng thử lại sau.");
            }

            return View(model);
        }

        private async Task<CheckoutViewModel> RebuildCheckoutModelAsync(CheckoutViewModel submitted)
        {
            var rebuilt = await _publicBookingService.BuildCheckoutAsync(
                submitted.RoomId,
                submitted.CheckIn,
                submitted.CheckOut,
                submitted.Guests,
                submitted.NumberOfRooms,
                submitted.RoomSelection);

            if (rebuilt == null)
            {
                return submitted;
            }

            rebuilt.CustomerName = submitted.CustomerName;
            rebuilt.Cccd = submitted.Cccd;
            rebuilt.PhoneNumber = submitted.PhoneNumber;
            rebuilt.Email = submitted.Email;
            rebuilt.Note = submitted.Note;
            rebuilt.NumberOfRooms = submitted.NumberOfRooms;
            rebuilt.RoomSelection = submitted.RoomSelection;
            rebuilt.PaymentMethod = rebuilt.VnPayAvailable ? submitted.PaymentMethod : PaymentMethods.PayAtHotel;
            return rebuilt;
        }

        private static object ToRoomsRoute(DateTime? checkIn, DateTime? checkOut, int? guests, string? roomType, int? rooms)
        {
            return new
            {
                checkIn = checkIn?.ToString("yyyy-MM-dd"),
                checkOut = checkOut?.ToString("yyyy-MM-dd"),
                guests,
                rooms,
                roomType = roomType?.Trim()
            };
        }

        private static bool IsDatabaseException(Exception ex)
        {
            return ex is DbException ||
                   ex is TimeoutException ||
                   ex is InvalidOperationException { InnerException: DbException } ||
                   ex.InnerException is DbException;
        }

        private bool IsVnPayConfigured()
        {
            return _vnPayOptions.Enabled &&
                   !string.IsNullOrWhiteSpace(_vnPayOptions.PaymentUrl) &&
                   !string.IsNullOrWhiteSpace(_vnPayOptions.TmnCode) &&
                   !string.IsNullOrWhiteSpace(_vnPayOptions.HashSecret);
        }
    }
}
