using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using PBL3.Models;
using PBL3.Services.Interfaces;

namespace PBL3.Controllers.Admin
{
    [Area("Admin")]
    public class DatPhongsController : Controller
    {
        private readonly IDatPhongService _datPhongService;
        private readonly IKhachHangService _khachHangService;
        private readonly INhanVienService _nhanVienService;

        public DatPhongsController(IDatPhongService datPhongService, IKhachHangService khachHangService, INhanVienService nhanVienService)
        {
            _datPhongService = datPhongService;
            _khachHangService = khachHangService;
            _nhanVienService = nhanVienService;
        }

        public async Task<IActionResult> Index()
        {
            return View(await _datPhongService.GetAllAsync());
        }

        public async Task<IActionResult> Details(string id)
        {
            if (id == null) return NotFound();

            var datPhong = await _datPhongService.GetByIdAsync(id);
            if (datPhong == null) return NotFound();

            return View(datPhong);
        }

        public async Task<IActionResult> Create()
        {
            ViewData["MaKh"] = new SelectList(await _khachHangService.GetAllAsync(), "MaKh", "HoTen");
            ViewData["MaNv"] = new SelectList(await _nhanVienService.GetAllAsync(), "MaNv", "HoTen");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("MaDatPhong,MaKh,MaNv,TenKhSnapshot,CccdSnapshot,SdtSnapshot,NgayDat,NgayNhanPhong,NgayTraPhong,TrangThai,GhiChu")] DatPhong datPhong)
        {
            ModelState.Remove("MaKhNavigation");
            ModelState.Remove("MaNvNavigation");

            if (datPhong.NgayNhanPhong >= datPhong.NgayTraPhong)
            {
                ModelState.AddModelError("NgayTraPhong", "Ngày trả phòng phải lớn hơn ngày nhận phòng.");
            }

            if (ModelState.IsValid)
            {
                if (await _datPhongService.KiemTraTrungMaAsync(datPhong.MaDatPhong))
                {
                    ModelState.AddModelError("MaDatPhong", "Mã đặt phòng đã tồn tại.");
                }
                else
                {
                    var createResult = await _datPhongService.CreateAsync(datPhong);
                    if (createResult)
                    {
                        return RedirectToAction(nameof(Index));
                    }

                    ModelState.AddModelError(string.Empty, "Không thể tạo đặt phòng. Vui lòng kiểm tra khách hàng, nhân viên, trạng thái hoặc dữ liệu liên quan.");
                }
            }

            ViewData["MaKh"] = new SelectList(await _khachHangService.GetAllAsync(), "MaKh", "HoTen", datPhong.MaKh);
            ViewData["MaNv"] = new SelectList(await _nhanVienService.GetAllAsync(), "MaNv", "HoTen", datPhong.MaNv);
            return View(datPhong);
        }

        public async Task<IActionResult> Edit(string id)
        {
            if (id == null) return NotFound();

            var datPhong = await _datPhongService.GetByIdAsync(id);
            if (datPhong == null) return NotFound();

            ViewData["MaKh"] = new SelectList(await _khachHangService.GetAllAsync(), "MaKh", "HoTen", datPhong.MaKh);
            ViewData["MaNv"] = new SelectList(await _nhanVienService.GetAllAsync(), "MaNv", "HoTen", datPhong.MaNv);
            return View(datPhong);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, [Bind("MaDatPhong,MaKh,MaNv,TenKhSnapshot,CccdSnapshot,SdtSnapshot,NgayDat,NgayNhanPhong,NgayTraPhong,TrangThai,GhiChu")] DatPhong datPhong)
        {
            if (id != datPhong.MaDatPhong) return NotFound();

            ModelState.Remove("MaKhNavigation");
            ModelState.Remove("MaNvNavigation");

            if (datPhong.NgayNhanPhong >= datPhong.NgayTraPhong)
            {
                ModelState.AddModelError("NgayTraPhong", "Ngày trả phòng phải lớn hơn ngày nhận phòng.");
            }

            if (ModelState.IsValid)
            {
                var updateResult = await _datPhongService.UpdateAsync(datPhong);
                if (updateResult)
                {
                    return RedirectToAction(nameof(Index));
                }

                ModelState.AddModelError(string.Empty, "Không thể cập nhật đặt phòng. Vui lòng kiểm tra dữ liệu liên quan.");
            }

            ViewData["MaKh"] = new SelectList(await _khachHangService.GetAllAsync(), "MaKh", "HoTen", datPhong.MaKh);
            ViewData["MaNv"] = new SelectList(await _nhanVienService.GetAllAsync(), "MaNv", "HoTen", datPhong.MaNv);
            return View(datPhong);
        }

        public async Task<IActionResult> Delete(string id)
        {
            if (id == null) return NotFound();

            var datPhong = await _datPhongService.GetByIdAsync(id);
            if (datPhong == null) return NotFound();

            return View(datPhong);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            var deleteResult = await _datPhongService.DeleteAsync(id);
            if (!deleteResult)
            {
                TempData["Error"] = "Không thể xóa đặt phòng vì dữ liệu đang được sử dụng hoặc không còn tồn tại.";
            }
            return RedirectToAction(nameof(Index));
        }
    }
}

