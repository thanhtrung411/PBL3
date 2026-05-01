using Microsoft.AspNetCore.Mvc;

namespace PBL3.Controllers
{
    public class ServiceController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
