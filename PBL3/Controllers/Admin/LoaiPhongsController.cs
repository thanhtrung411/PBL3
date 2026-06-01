using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using PBL3.Models;
using PBL3.Services.Interfaces;

namespace PBL3.Controllers.Admin
{
    [Area("Admin")]
    public class LoaiPhongsController : Controller
    {
        private readonly ILoaiPhongService _loaiPhongService;

        public LoaiPhongsController(ILoaiPhongService loaiPhongService)
        {
            _loaiPhongService = loaiPhongService;
        }

        // GET: LoaiPhongs
        public async Task<IActionResult> Index()
        {
            var dsLoaiPhong = await _loaiPhongService.GetAllLoaiPhongsAsync();
            return View(dsLoaiPhong);
        }

        // GET: LoaiPhongs/Details/5
        public async Task<IActionResult> Details(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var loaiPhong = await _loaiPhongService.GetLoaiPhongByIdAsync(id);
            if (loaiPhong == null)
            {
                return NotFound();
            }

            return View(loaiPhong);
        }

        // GET: LoaiPhongs/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: LoaiPhongs/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("MaLoaiPhong,TenLoaiPhong,SoNguoiToiDa,MoTa")] LoaiPhong loaiPhong)
        {
            if (!ModelState.IsValid)
            {
                return View(loaiPhong);
            }

            if (await _loaiPhongService.KiemTraTrungMaAsync(loaiPhong.MaLoaiPhong))
            {
                ModelState.AddModelError("MaLoaiPhong", "Mã loại phòng đã tồn tại.");
                return View(loaiPhong);
            }

            if (await _loaiPhongService.KiemTraTrungTenAsync(loaiPhong.TenLoaiPhong))
            {
                ModelState.AddModelError("TenLoaiPhong", "Tên loại phòng đã tồn tại.");
                return View(loaiPhong);
            }

            var createResult = await _loaiPhongService.CreateLoaiPhongAsync(loaiPhong);
            if (createResult)
            {
                return RedirectToAction(nameof(Index));
            }

            ModelState.AddModelError(string.Empty, "Không thể tạo loại phòng. Vui lòng kiểm tra dữ liệu liên quan.");
            return View(loaiPhong);
        }

        // GET: LoaiPhongs/Edit/5
        public async Task<IActionResult> Edit(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var loaiPhong = await _loaiPhongService.GetLoaiPhongByIdAsync(id);
            if (loaiPhong == null)
            {
                return NotFound();
            }
            return View(loaiPhong);
        }

        // POST: LoaiPhongs/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, [Bind("MaLoaiPhong,TenLoaiPhong,SoNguoiToiDa,MoTa")] LoaiPhong loaiPhong)
        {
            if (id != loaiPhong.MaLoaiPhong)
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                return View(loaiPhong);
            }

            if (await _loaiPhongService.KiemTraTrungTenAsync(loaiPhong.TenLoaiPhong, loaiPhong.MaLoaiPhong))
            {
                ModelState.AddModelError("TenLoaiPhong", "Tên loại phòng đã tồn tại.");
                return View(loaiPhong);
            }

            var updateResult = await _loaiPhongService.UpdateLoaiPhongAsync(loaiPhong);
            if (updateResult)
            {
                return RedirectToAction(nameof(Index));
            }

            ModelState.AddModelError(string.Empty, "Không thể cập nhật loại phòng. Vui lòng kiểm tra dữ liệu liên quan.");
            return View(loaiPhong);
        }

        // GET: LoaiPhongs/Delete/5
        public async Task<IActionResult> Delete(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var loaiPhong = await _loaiPhongService.GetLoaiPhongByIdAsync(id);
            if (loaiPhong == null)
            {
                return NotFound();
            }

            return View(loaiPhong);
        }

        // POST: LoaiPhongs/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            var deleteResult = await _loaiPhongService.DeleteLoaiPhongAsync(id);
            if (!deleteResult)
            {
                TempData["Error"] = "Không thể xóa loại phòng vì dữ liệu đang được sử dụng hoặc không còn tồn tại.";
            }
            return RedirectToAction(nameof(Index));
        }
    }
}

