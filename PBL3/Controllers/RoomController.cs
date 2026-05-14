//using Microsoft.AspNetCore.Mvc;

//namespace PBL3.Controllers
//{
//    public class RoomController : Controller
//    {
//        public IActionResult Index()
//        {
//            return View();
//        }

//        [HttpPost]
//        public IActionResult Checkout(string roomNumber)
//        {
//            // In a real application, you would update the database here.
//            // For now, we just return success.
//            return Json(new { success = true, message = "Trả phòng thành công" });
//        }
//    }
//}
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
            // Transitions: Occupied -> Cleaning
            return Json(new { success = true, message = "Trả phòng thành công. Phòng đang dọn dẹp." });
        }

        [HttpPost]
        public IActionResult FinishTask(string roomNumber, string status)
        {
            // Transitions: Cleaning/Maintenance -> Available or Maintenance
            string msg = status == "available" ? "Phòng đã sẵn sàng" : "Đã chuyển sang bảo trì";
            return Json(new { success = true, message = msg });
        }

        [HttpPost]
        public IActionResult BookRoom(string roomNumber, string status)
        {
            // Transitions: Available -> Occupied or Reserved
            string msg = status == "occupied" ? "Nhận phòng thành công" : "Đặt giữ phòng thành công";
            return Json(new { success = true, message = msg });
        }
    }
}
