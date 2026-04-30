using Microsoft.AspNetCore.Mvc;
using PBL3.Models;
using PBL3.Services.Interfaces;

namespace PBL3.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class VaiTrosController : Controller
    {
        private readonly IVaiTroService _vaiTroService;

        public VaiTrosController(IVaiTroService vaiTroService)
        {
            _vaiTroService = vaiTroService;
        }

        // GET: VaiTros
        public async Task<IActionResult> Index()
        {
            var dsVaiTro = await _vaiTroService.GetAllAsync();
            return View(dsVaiTro);
        }

        // GET: VaiTros/Details/5
        public async Task<IActionResult> Details(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return NotFound();
            }

            var vaiTro = await _vaiTroService.GetByIdAsync(id);
            if (vaiTro == null)
            {
                return NotFound();
            }

            return View(vaiTro);
        }

        // GET: VaiTros/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: VaiTros/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("MaVaiTro,TenVaiTro,MoTa")] VaiTro vaiTro)
        {
            if (!ModelState.IsValid)
            {
                return View(vaiTro);
            }

            if (await _vaiTroService.KiemTraTrungMaAsync(vaiTro.MaVaiTro))
            {
                ModelState.AddModelError("MaVaiTro", "Mã vai trò đã tồn tại.");
                return View(vaiTro);
            }

            if (await _vaiTroService.KiemTraTrungTenAsync(vaiTro.TenVaiTro))
            {
                ModelState.AddModelError("TenVaiTro", "Tên vai trò đã tồn tại.");
                return View(vaiTro);
            }

            var createResult = await _vaiTroService.CreateAsync(vaiTro);
            if (createResult)
            {
                return RedirectToAction(nameof(Index));
            }

            ModelState.AddModelError(string.Empty, "Không thể tạo vai trò. Vui lòng kiểm tra dữ liệu liên quan.");
            return View(vaiTro);
        }

        // GET: VaiTros/Edit/5
        public async Task<IActionResult> Edit(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return NotFound();
            }

            var vaiTro = await _vaiTroService.GetByIdAsync(id);
            if (vaiTro == null)
            {
                return NotFound();
            }

            return View(vaiTro);
        }

        // POST: VaiTros/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, [Bind("MaVaiTro,TenVaiTro,MoTa")] VaiTro vaiTro)
        {
            if (id != vaiTro.MaVaiTro)
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                return View(vaiTro);
            }

            if (await _vaiTroService.KiemTraTrungTenAsync(vaiTro.TenVaiTro, vaiTro.MaVaiTro))
            {
                ModelState.AddModelError("TenVaiTro", "Tên vai trò đã tồn tại.");
                return View(vaiTro);
            }

            var updateResult = await _vaiTroService.UpdateAsync(vaiTro);
            if (updateResult)
            {
                return RedirectToAction(nameof(Index));
            }

            ModelState.AddModelError(string.Empty, "Không thể cập nhật vai trò. Vui lòng kiểm tra dữ liệu liên quan.");
            return View(vaiTro);
        }

        // GET: VaiTros/Delete/5
        public async Task<IActionResult> Delete(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return NotFound();
            }

            var vaiTro = await _vaiTroService.GetByIdAsync(id);
            if (vaiTro == null)
            {
                return NotFound();
            }

            return View(vaiTro);
        }

        // POST: VaiTros/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            var deleteResult = await _vaiTroService.DeleteAsync(id);
            if (!deleteResult)
            {
                TempData["Error"] = "Không thể xóa vai trò vì dữ liệu đang được sử dụng hoặc không còn tồn tại.";
            }
            return RedirectToAction(nameof(Index));
        }
    }
}

