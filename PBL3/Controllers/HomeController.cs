using Microsoft.AspNetCore.Mvc;
using PBL3.Models;
using PBL3.Services.Interfaces;
using System.Diagnostics;

namespace PBL3.Controllers
{
    public class HomeController : Controller
    {
        private readonly IAdminDashboardService _adminDashboardService;

        public HomeController(IAdminDashboardService adminDashboardService)
        {
            _adminDashboardService = adminDashboardService;
        }

        public async Task<IActionResult> Index()
        {
            var model = await _adminDashboardService.GetOverviewAsync(HttpContext.RequestAborted);
            return View(model);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
