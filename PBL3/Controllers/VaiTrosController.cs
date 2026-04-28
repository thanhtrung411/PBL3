using Microsoft.AspNetCore.Mvc;
using PBL3.Models;
using PBL3.Services.Interfaces;

namespace PBL3.Controllers
{
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

            await _vaiTroService.CreateAsync(vaiTro);
            return RedirectToAction(nameof(Index));
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

            await _vaiTroService.UpdateAsync(vaiTro);
            return RedirectToAction(nameof(Index));
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
            await _vaiTroService.DeleteAsync(id);
            return RedirectToAction(nameof(Index));
        }
    }
}
