using Microsoft.AspNetCore.Mvc;

namespace PBL3.Controllers
{
    public class BookingManagementController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
