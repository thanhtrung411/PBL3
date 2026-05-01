using Microsoft.AspNetCore.Mvc;

namespace PBL3.Controllers
{
    public class CustomerController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
