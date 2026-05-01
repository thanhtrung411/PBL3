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
        public IActionResult Checkout(int roomId)
        {
            return RedirectToAction("Checkout", "Booking", new { roomId });
        }

        [HttpPost]
        public IActionResult ProcessPayment(
            string customerName,
            string email,
            string phoneNumber,
            string? note,
            string? paymentMethod)
        {
            var bookingCode = $"BK{DateTime.Now:yyyyMMddHHmmss}";
            return RedirectToAction("Success", "Booking", new { id = bookingCode });
        }

        public IActionResult BookingSuccess(string? id)
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
