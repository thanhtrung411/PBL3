using Microsoft.AspNetCore.Mvc;

namespace PBL3.Controllers
{
    public class GuestController : Controller
    {
        // GET: /Guest/Index
        public IActionResult Index()
        {
            return View();
        }
    }
}
