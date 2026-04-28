using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using PBL3.Models;
using PBL3.Services.Interfaces;

namespace PBL3.Controllers
{
    public class PhongsController : Controller
    {
        private readonly IPhongService _phongService;
        private readonly ILoaiPhongService _loaiPhongService;

        public PhongsController(IPhongService phongService, ILoaiPhongService loaiPhongService)
        {
            _phongService = phongService;
            _loaiPhongService = loaiPhongService;
        }

        // GET: Phongs
        public async Task<IActionResult> Index()
        {
            var dsPhong = await _phongService.GetAllAsync();
            return View(dsPhong);
        }

        // GET: Phongs/Details/5
        public async Task<IActionResult> Details(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return NotFound();
            }

            var phong = await _phongService.GetByIdAsync(id);
            if (phong == null)
            {
                return NotFound();
            }

            return View(phong);
        }

        // GET: Phongs/Create
        public async Task<IActionResult> Create()
        {
            var dsLoaiPhong = await _loaiPhongService.GetAllLoaiPhongsAsync();
            ViewBag.MaLoaiPhong = new SelectList(dsLoaiPhong, "MaLoaiPhong", "TenLoaiPhong");
            return View();
        }

        // POST: Phongs/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("MaPhong,SoPhong,MaLoaiPhong,Tang,TrangThai,GhiChu")] Phong phong)
        {
            ModelState.Remove("MaLoaiPhongNavigation");

            if (!ModelState.IsValid)
            {
                var dsLoaiPhong = await _loaiPhongService.GetAllLoaiPhongsAsync();
                ViewBag.MaLoaiPhong = new SelectList(dsLoaiPhong, "MaLoaiPhong", "TenLoaiPhong", phong.MaLoaiPhong);
                return View(phong);
            }

            if (await _phongService.KiemTraTrungMaAsync(phong.MaPhong))
            {
                ModelState.AddModelError("MaPhong", "Mã phòng đã tồn tại.");
                var dsLoaiPhong = await _loaiPhongService.GetAllLoaiPhongsAsync();
                ViewBag.MaLoaiPhong = new SelectList(dsLoaiPhong, "MaLoaiPhong", "TenLoaiPhong", phong.MaLoaiPhong);
                return View(phong);
            }

            if (await _phongService.KiemTraTrungSoPhongAsync(phong.SoPhong))
            {
                ModelState.AddModelError("SoPhong", "Số phòng đã tồn tại.");
                var dsLoaiPhong = await _loaiPhongService.GetAllLoaiPhongsAsync();
                ViewBag.MaLoaiPhong = new SelectList(dsLoaiPhong, "MaLoaiPhong", "TenLoaiPhong", phong.MaLoaiPhong);
                return View(phong);
            }

            await _phongService.CreateAsync(phong);
            return RedirectToAction(nameof(Index));
        }

        // GET: Phongs/Edit/5
        public async Task<IActionResult> Edit(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return NotFound();
            }

            var phong = await _phongService.GetByIdAsync(id);
            if (phong == null)
            {
                return NotFound();
            }

            var dsLoaiPhong = await _loaiPhongService.GetAllLoaiPhongsAsync();
            ViewBag.MaLoaiPhong = new SelectList(dsLoaiPhong, "MaLoaiPhong", "TenLoaiPhong", phong.MaLoaiPhong);
            return View(phong);
        }

        // POST: Phongs/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, [Bind("MaPhong,SoPhong,MaLoaiPhong,Tang,TrangThai,GhiChu")] Phong phong)
        {
            if (id != phong.MaPhong)
            {
                return NotFound();
            }

            ModelState.Remove("MaLoaiPhongNavigation");

            if (!ModelState.IsValid)
            {
                var dsLoaiPhong = await _loaiPhongService.GetAllLoaiPhongsAsync();
                ViewBag.MaLoaiPhong = new SelectList(dsLoaiPhong, "MaLoaiPhong", "TenLoaiPhong", phong.MaLoaiPhong);
                return View(phong);
            }

            if (await _phongService.KiemTraTrungSoPhongAsync(phong.SoPhong, phong.MaPhong))
            {
                ModelState.AddModelError("SoPhong", "Số phòng đã tồn tại.");
                var dsLoaiPhong = await _loaiPhongService.GetAllLoaiPhongsAsync();
                ViewBag.MaLoaiPhong = new SelectList(dsLoaiPhong, "MaLoaiPhong", "TenLoaiPhong", phong.MaLoaiPhong);
                return View(phong);
            }

            await _phongService.UpdateAsync(phong);
            return RedirectToAction(nameof(Index));
        }

        // GET: Phongs/Delete/5
        public async Task<IActionResult> Delete(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return NotFound();
            }

            var phong = await _phongService.GetByIdAsync(id);
            if (phong == null)
            {
                return NotFound();
            }

            return View(phong);
        }

        // POST: Phongs/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            await _phongService.DeleteAsync(id);
            return RedirectToAction(nameof(Index));
        }
    }
}