using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace PBL3.Controllers
{
    [AllowAnonymous]
    public class GuestController : Controller
    {
        public IActionResult Index()
        {
            return Redirect("/Booking");
        }

        public IActionResult Rooms()
        {
            return RedirectToAction("Rooms", "Booking", ToRouteValues());
        }

        [HttpGet]
        public IActionResult Checkout(string? roomId)
        {
            var values = ToRouteValues();
            if (!string.IsNullOrWhiteSpace(roomId))
            {
                values["roomId"] = roomId;
            }

            return RedirectToAction("Checkout", "Booking", values);
        }

        [HttpPost]
        public IActionResult ProcessPayment(
            string customerName,
            string email,
            string phoneNumber,
            string? note,
            string? paymentMethod)
        {
            TempData["Error"] = "Vui lòng đặt phòng qua biểu mẫu xác nhận mới để hệ thống tạo giữ chỗ trong database.";
            return RedirectToAction("Index", "Booking");
        }

        public IActionResult BookingSuccess(string? id)
        {
            return RedirectToAction("Success", "Booking", new { id });
        }

        public IActionResult Success(string? id)
        {
            return RedirectToAction("Success", "Booking", new { id });
        }

        private RouteValueDictionary ToRouteValues()
        {
            var values = new RouteValueDictionary();
            foreach (var item in Request.Query)
            {
                values[item.Key] = item.Value.ToString();
            }

            return values;
        }
    }
}
