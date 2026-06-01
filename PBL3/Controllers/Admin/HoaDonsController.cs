using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using PBL3.Models;
using PBL3.Services.Interfaces;

namespace PBL3.Controllers.Admin
{
    [Area("Admin")]
    public class HoaDonsController : Controller
    {
        private readonly IHoaDonService _hoaDonService;
        private readonly IDatPhongService _datPhongService;
        private readonly IMaGiamGiaService _maGiamGiaService;

        public HoaDonsController(IHoaDonService hoaDonService, IDatPhongService datPhongService, IMaGiamGiaService maGiamGiaService)
        {
            _hoaDonService = hoaDonService;
            _datPhongService = datPhongService;
            _maGiamGiaService = maGiamGiaService;
        }

        public async Task<IActionResult> Index()
        {
            return View(await _hoaDonService.GetAllAsync());
        }

        public async Task<IActionResult> Details(string id)
        {
            if (id == null) return NotFound();

            var hoaDon = await _hoaDonService.GetByIdAsync(id);
            if (hoaDon == null) return NotFound();

            return View(hoaDon);
        }

        public async Task<IActionResult> Create()
        {
            ViewData["MaDatPhong"] = new SelectList(await _datPhongService.GetAllAsync(), "MaDatPhong", "MaDatPhong");
            ViewData["MaGiamGiaPhong"] = new SelectList(await _maGiamGiaService.GetAllAsync(), "MaGiamGia", "TenMaGiamGia");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("MaHoaDon,MaDatPhong,TongTienPhong,TongTienDichVu,TienDatCoc,MaGiamGiaPhong,TienGiamGiaPhong,TongThanhToan,SoTienDaThanhToan,NgayThanhToanCuoi,PhuongThucThanhToan,TrangThai,GhiChu")] HoaDon hoaDon)
        {
            ModelState.Remove("MaDatPhongNavigation");
            ModelState.Remove("MaGiamGiaPhongNavigation");

            if (string.IsNullOrWhiteSpace(hoaDon.MaGiamGiaPhong))
            {
                hoaDon.MaGiamGiaPhong = null;
            }

            if (ModelState.IsValid)
            {
                if (await _hoaDonService.KiemTraTrungMaAsync(hoaDon.MaHoaDon))
                {
                    ModelState.AddModelError("MaHoaDon", "Mã hóa đơn đã tồn tại.");
                }
                else
                {
                    try
                    {
                        var createResult = await _hoaDonService.CreateAsync(hoaDon);
                        if (createResult)
                        {
                            return RedirectToAction(nameof(Index));
                        }

                        ModelState.AddModelError(string.Empty, "Không thể tạo hóa đơn. Vui lòng kiểm tra dữ liệu liên quan.");
                    }
                    catch (DbUpdateException)
                    {
                        ModelState.AddModelError("MaDatPhong", "Không thể tạo hóa đơn. Đặt phòng không tồn tại hoặc đã có hóa đơn.");
                    }
                }
            }

            ViewData["MaDatPhong"] = new SelectList(await _datPhongService.GetAllAsync(), "MaDatPhong", "MaDatPhong", hoaDon.MaDatPhong);
            ViewData["MaGiamGiaPhong"] = new SelectList(await _maGiamGiaService.GetAllAsync(), "MaGiamGia", "TenMaGiamGia", hoaDon.MaGiamGiaPhong);
            return View(hoaDon);
        }

        public async Task<IActionResult> Edit(string id)
        {
            if (id == null) return NotFound();

            var hoaDon = await _hoaDonService.GetByIdAsync(id);
            if (hoaDon == null) return NotFound();

            ViewData["MaDatPhong"] = new SelectList(await _datPhongService.GetAllAsync(), "MaDatPhong", "MaDatPhong", hoaDon.MaDatPhong);
            ViewData["MaGiamGiaPhong"] = new SelectList(await _maGiamGiaService.GetAllAsync(), "MaGiamGia", "TenMaGiamGia", hoaDon.MaGiamGiaPhong);
            return View(hoaDon);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, [Bind("MaHoaDon,MaDatPhong,TongTienPhong,TongTienDichVu,TienDatCoc,MaGiamGiaPhong,TienGiamGiaPhong,TongThanhToan,SoTienDaThanhToan,NgayThanhToanCuoi,PhuongThucThanhToan,TrangThai,GhiChu")] HoaDon hoaDon)
        {
            if (id != hoaDon.MaHoaDon) return NotFound();

            ModelState.Remove("MaDatPhongNavigation");
            ModelState.Remove("MaGiamGiaPhongNavigation");

            if (string.IsNullOrWhiteSpace(hoaDon.MaGiamGiaPhong))
            {
                hoaDon.MaGiamGiaPhong = null;
            }

            if (ModelState.IsValid)
            {
                var updateResult = await _hoaDonService.UpdateAsync(hoaDon);
                if (updateResult)
                {
                    return RedirectToAction(nameof(Index));
                }

                ModelState.AddModelError(string.Empty, "Không thể cập nhật hóa đơn. Vui lòng kiểm tra dữ liệu liên quan.");
            }

            ViewData["MaDatPhong"] = new SelectList(await _datPhongService.GetAllAsync(), "MaDatPhong", "MaDatPhong", hoaDon.MaDatPhong);
            ViewData["MaGiamGiaPhong"] = new SelectList(await _maGiamGiaService.GetAllAsync(), "MaGiamGia", "TenMaGiamGia", hoaDon.MaGiamGiaPhong);
            return View(hoaDon);
        }

        public async Task<IActionResult> Delete(string id)
        {
            if (id == null) return NotFound();

            var hoaDon = await _hoaDonService.GetByIdAsync(id);
            if (hoaDon == null) return NotFound();

            return View(hoaDon);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            var deleteResult = await _hoaDonService.DeleteAsync(id);
            if (!deleteResult)
            {
                TempData["Error"] = "Không thể xóa hóa đơn vì dữ liệu đang được sử dụng hoặc không còn tồn tại.";
            }
            return RedirectToAction(nameof(Index));
        }
    }
}

