using Microsoft.AspNetCore.Mvc;

namespace PBL3.Controllers
{
    public class RoomController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Checkout(string roomNumber)
        {
            // In a real application, you would update the database here.
            // For now, we just return success.
            return Json(new { success = true, message = "Trả phòng thành công" });
        }
    }
}
