using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using PBL3.Models;
using PBL3.Services.Interfaces;

namespace PBL3.Controllers
{
    public class BangGiaPhongsController : Controller
    {
        private readonly IBangGiaPhongService _bangGiaPhongService;
        private readonly ILoaiPhongService _loaiPhongService;

        public BangGiaPhongsController(IBangGiaPhongService bangGiaPhongService, ILoaiPhongService loaiPhongService)
        {
            _bangGiaPhongService = bangGiaPhongService;
            _loaiPhongService = loaiPhongService;
        }

        public async Task<IActionResult> Index()
        {
            var data = await _bangGiaPhongService.GetAllAsync();
            return View(data);
        }

        public async Task<IActionResult> Details(string id)
        {
            if (id == null) return NotFound();

            var bangGiaPhong = await _bangGiaPhongService.GetByIdAsync(id);
            if (bangGiaPhong == null) return NotFound();

            return View(bangGiaPhong);
        }

        public async Task<IActionResult> Create()
        {
            ViewData["MaLoaiPhong"] = new SelectList(await _loaiPhongService.GetAllLoaiPhongsAsync(), "MaLoaiPhong", "TenLoaiPhong");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("MaBangGia,MaLoaiPhong,TuNgay,DenNgay,ThuApDung,GiaApDung,LoaiGia,UuTien,TrangThai,GhiChu")] BangGiaPhong bangGiaPhong)
        {
            if (bangGiaPhong.TuNgay > bangGiaPhong.DenNgay)
            {
                ModelState.AddModelError("DenNgay", "Từ ngày phải nhỏ hơn hoặc bằng Đến ngày.");
            }

            ModelState.Remove("MaLoaiPhongNavigation");

            if (ModelState.IsValid)
            {
                if (await _bangGiaPhongService.KiemTraTrungMaAsync(bangGiaPhong.MaBangGia))
                {
                    ModelState.AddModelError("MaBangGia", "Mã bảng giá đã tồn tại.");
                }
                else
                {
                    await _bangGiaPhongService.CreateAsync(bangGiaPhong);
                    return RedirectToAction(nameof(Index));
                }
            }
            ViewData["MaLoaiPhong"] = new SelectList(await _loaiPhongService.GetAllLoaiPhongsAsync(), "MaLoaiPhong", "TenLoaiPhong", bangGiaPhong.MaLoaiPhong);
            return View(bangGiaPhong);
        }

        public async Task<IActionResult> Edit(string id)
        {
            if (id == null) return NotFound();

            var bangGiaPhong = await _bangGiaPhongService.GetByIdAsync(id);
            if (bangGiaPhong == null) return NotFound();

            ViewData["MaLoaiPhong"] = new SelectList(await _loaiPhongService.GetAllLoaiPhongsAsync(), "MaLoaiPhong", "TenLoaiPhong", bangGiaPhong.MaLoaiPhong);
            return View(bangGiaPhong);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, [Bind("MaBangGia,MaLoaiPhong,TuNgay,DenNgay,ThuApDung,GiaApDung,LoaiGia,UuTien,TrangThai,GhiChu")] BangGiaPhong bangGiaPhong)
        {
            if (id != bangGiaPhong.MaBangGia) return NotFound();

            if (bangGiaPhong.TuNgay > bangGiaPhong.DenNgay)
            {
                ModelState.AddModelError("DenNgay", "Từ ngày phải nhỏ hơn hoặc bằng Đến ngày.");
            }

            ModelState.Remove("MaLoaiPhongNavigation");

            if (ModelState.IsValid)
            {
                await _bangGiaPhongService.UpdateAsync(bangGiaPhong);
                return RedirectToAction(nameof(Index));
            }
            ViewData["MaLoaiPhong"] = new SelectList(await _loaiPhongService.GetAllLoaiPhongsAsync(), "MaLoaiPhong", "TenLoaiPhong", bangGiaPhong.MaLoaiPhong);
            return View(bangGiaPhong);
        }

        public async Task<IActionResult> Delete(string id)
        {
            if (id == null) return NotFound();

            var bangGiaPhong = await _bangGiaPhongService.GetByIdAsync(id);
            if (bangGiaPhong == null) return NotFound();

            return View(bangGiaPhong);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            await _bangGiaPhongService.DeleteAsync(id);
            return RedirectToAction(nameof(Index));
        }
    }
}
