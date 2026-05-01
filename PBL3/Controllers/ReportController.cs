using Microsoft.AspNetCore.Mvc;

namespace PBL3.Controllers
{
    public class ReportController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
