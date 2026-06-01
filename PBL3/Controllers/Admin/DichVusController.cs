using Microsoft.AspNetCore.Mvc;
using PBL3.Models;
using PBL3.Services.Interfaces;

namespace PBL3.Controllers.Admin
{
    [Area("Admin")]
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

            var createResult = await _dichVuService.CreateAsync(dichVu);
            if (createResult)
            {
                return RedirectToAction(nameof(Index));
            }

            ModelState.AddModelError(string.Empty, "Không thể tạo dịch vụ. Vui lòng kiểm tra dữ liệu liên quan.");
            return View(dichVu);
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

            var updateResult = await _dichVuService.UpdateAsync(dichVu);
            if (updateResult)
            {
                return RedirectToAction(nameof(Index));
            }

            ModelState.AddModelError(string.Empty, "Không thể cập nhật dịch vụ. Vui lòng kiểm tra dữ liệu liên quan.");
            return View(dichVu);
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
            var deleteResult = await _dichVuService.DeleteAsync(id);
            if (!deleteResult)
            {
                TempData["Error"] = "Không thể xóa dịch vụ vì dữ liệu đang được sử dụng hoặc không còn tồn tại.";
            }
            return RedirectToAction(nameof(Index));
        }
    }
}

