using Microsoft.AspNetCore.Mvc;
using PBL3.Models;
using PBL3.Services.Interfaces;

namespace PBL3.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class MaGiamGiasController : Controller
    {
        private readonly IMaGiamGiaService _maGiamGiaService;

        public MaGiamGiasController(IMaGiamGiaService maGiamGiaService)
        {
            _maGiamGiaService = maGiamGiaService;
        }

        public async Task<IActionResult> Index()
        {
            return View(await _maGiamGiaService.GetAllAsync());
        }

        public async Task<IActionResult> Details(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return NotFound();
            var maGiamGia = await _maGiamGiaService.GetByIdAsync(id);
            if (maGiamGia == null) return NotFound();
            return View(maGiamGia);
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("MaGiamGia,CodeGiamGia,TenMaGiamGia,LoaiGiamGia,GiaTriGiam,HoaDonToiThieu,GiamToiDa,PhamViApDung,TuNgay,DenNgay,SoLuongPhatHanh,SoLuongDaDung,TrangThai,MoTa,GhiChu")] MaGiamGium model)
        {
            if (model.TuNgay > model.DenNgay)
            {
                ModelState.AddModelError("DenNgay", "Từ ngày phải nhỏ hơn hoặc bằng Đến ngày.");
            }

            if (!ModelState.IsValid) return View(model);

            if (await _maGiamGiaService.KiemTraTrungMaAsync(model.MaGiamGia))
            {
                ModelState.AddModelError("MaGiamGia", "Mã giảm giá đã tồn tại trong hệ thống.");
                return View(model);
            }

            if (await _maGiamGiaService.KiemTraTrungCodeAsync(model.CodeGiamGia))
            {
                ModelState.AddModelError("CodeGiamGia", "Code giảm giá đã được sử dụng.");
                return View(model);
            }

            var createResult = await _maGiamGiaService.CreateAsync(model);
            if (createResult)
            {
                return RedirectToAction(nameof(Index));
            }

            ModelState.AddModelError(string.Empty, "Không thể tạo mã giảm giá. Vui lòng kiểm tra dữ liệu liên quan.");
            return View(model);
        }

        public async Task<IActionResult> Edit(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return NotFound();
            var maGiamGia = await _maGiamGiaService.GetByIdAsync(id);
            if (maGiamGia == null) return NotFound();
            return View(maGiamGia);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, [Bind("MaGiamGia,CodeGiamGia,TenMaGiamGia,LoaiGiamGia,GiaTriGiam,HoaDonToiThieu,GiamToiDa,PhamViApDung,TuNgay,DenNgay,SoLuongPhatHanh,SoLuongDaDung,TrangThai,MoTa,GhiChu")] MaGiamGium model)
        {
            if (id != model.MaGiamGia) return NotFound();

            if (model.TuNgay > model.DenNgay)
            {
                ModelState.AddModelError("DenNgay", "Từ ngày phải nhỏ hơn hoặc bằng Đến ngày.");
            }

            if (!ModelState.IsValid) return View(model);

            if (await _maGiamGiaService.KiemTraTrungCodeAsync(model.CodeGiamGia, model.MaGiamGia))
            {
                ModelState.AddModelError("CodeGiamGia", "Code giảm giá đã được sử dụng.");
                return View(model);
            }

            var updateResult = await _maGiamGiaService.UpdateAsync(model);
            if (updateResult)
            {
                return RedirectToAction(nameof(Index));
            }

            ModelState.AddModelError(string.Empty, "Không thể cập nhật mã giảm giá. Vui lòng kiểm tra dữ liệu liên quan.");
            return View(model);
        }

        public async Task<IActionResult> Delete(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return NotFound();
            var maGiamGia = await _maGiamGiaService.GetByIdAsync(id);
            if (maGiamGia == null) return NotFound();
            return View(maGiamGia);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            var deleteResult = await _maGiamGiaService.DeleteAsync(id);
            if (!deleteResult)
            {
                TempData["Error"] = "Không thể xóa mã giảm giá vì dữ liệu đang được sử dụng hoặc không còn tồn tại.";
            }
            return RedirectToAction(nameof(Index));
        }
    }
}

