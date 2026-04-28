using Microsoft.AspNetCore.Mvc;
using PBL3.Models;
using PBL3.Services.Interfaces;

namespace PBL3.Controllers
{
    public class DichVusController : Controller
    {
        private readonly IDichVuService _dichVuService;

        public DichVusController(IDichVuService dichVuService)
        {
            _dichVuService = dichVuService;
        }

        public async Task<IActionResult> Index()
        {
            return View(await _dichVuService.GetAllAsync());
        }

        public async Task<IActionResult> Details(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return NotFound();
            var dichVu = await _dichVuService.GetByIdAsync(id);
            if (dichVu == null) return NotFound();
            return View(dichVu);
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("MaDv,TenDv,DonGia,DonViTinh,TrangThai,GhiChu")] DichVu dichVu)
        {
            if (!ModelState.IsValid) return View(dichVu);

            if (await _dichVuService.KiemTraTrungMaAsync(dichVu.MaDv))
            {
                ModelState.AddModelError("MaDv", "Mã dịch vụ đã tồn tại.");
                return View(dichVu);
            }

            if (await _dichVuService.KiemTraTrungTenAsync(dichVu.TenDv))
            {
                ModelState.AddModelError("TenDv", "Tên dịch vụ đã tồn tại.");
                return View(dichVu);
            }

            await _dichVuService.CreateAsync(dichVu);
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return NotFound();
            var dichVu = await _dichVuService.GetByIdAsync(id);
            if (dichVu == null) return NotFound();
            return View(dichVu);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, [Bind("MaDv,TenDv,DonGia,DonViTinh,TrangThai,GhiChu")] DichVu dichVu)
        {
            if (id != dichVu.MaDv) return NotFound();

            if (!ModelState.IsValid) return View(dichVu);

            if (await _dichVuService.KiemTraTrungTenAsync(dichVu.TenDv, dichVu.MaDv))
            {
                ModelState.AddModelError("TenDv", "Tên dịch vụ đã tồn tại.");
                return View(dichVu);
            }

            await _dichVuService.UpdateAsync(dichVu);
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Delete(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return NotFound();
            var dichVu = await _dichVuService.GetByIdAsync(id);
            if (dichVu == null) return NotFound();
            return View(dichVu);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            await _dichVuService.DeleteAsync(id);
            return RedirectToAction(nameof(Index));
        }
    }
}
