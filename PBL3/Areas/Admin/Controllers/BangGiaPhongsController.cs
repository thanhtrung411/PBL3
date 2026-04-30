using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using PBL3.Models;
using PBL3.Services.Interfaces;

namespace PBL3.Areas.Admin.Controllers
{
    [Area("Admin")]
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
                    var createResult = await _bangGiaPhongService.CreateAsync(bangGiaPhong);
                    if (createResult)
                    {
                        return RedirectToAction(nameof(Index));
                    }

                    ModelState.AddModelError(string.Empty, "Không thể tạo bảng giá phòng. Vui lòng kiểm tra dữ liệu liên quan.");
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
                var updateResult = await _bangGiaPhongService.UpdateAsync(bangGiaPhong);
                if (updateResult)
                {
                    return RedirectToAction(nameof(Index));
                }

                ModelState.AddModelError(string.Empty, "Không thể cập nhật bảng giá phòng. Vui lòng kiểm tra dữ liệu liên quan.");
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
            var deleteResult = await _bangGiaPhongService.DeleteAsync(id);
            if (!deleteResult)
            {
                TempData["Error"] = "Không thể xóa bảng giá phòng vì dữ liệu đang được sử dụng hoặc không còn tồn tại.";
            }
            return RedirectToAction(nameof(Index));
        }
    }
}

