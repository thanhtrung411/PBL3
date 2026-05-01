using Microsoft.AspNetCore.Mvc;

namespace PBL3.Controllers
{
    public class InvoiceController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
