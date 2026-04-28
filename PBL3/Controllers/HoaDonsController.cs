using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using PBL3.Models;
using PBL3.Services.Interfaces;

namespace PBL3.Controllers
{
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
                    try {
                        await _hoaDonService.CreateAsync(hoaDon);
                        return RedirectToAction(nameof(Index));
                    } catch(Exception) {
                        ModelState.AddModelError("MaDatPhong", "Lỗi liên kết. Mỗi Đặt phòng chỉ có 1 hóa đơn duy nhất.");
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
                await _hoaDonService.UpdateAsync(hoaDon);
                return RedirectToAction(nameof(Index));
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
            await _hoaDonService.DeleteAsync(id);
            return RedirectToAction(nameof(Index));
        }
    }
}
