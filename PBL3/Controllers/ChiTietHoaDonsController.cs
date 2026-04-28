using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using PBL3.Models;
using PBL3.Services.Interfaces;

namespace PBL3.Controllers
{
    public class ChiTietHoaDonsController : Controller
    {
        private readonly IChiTietHoaDonService _chiTietHoaDonService;
        private readonly IHoaDonService _hoaDonService;
        private readonly IPhongService _phongService;
        private readonly IDichVuService _dichVuService;
        private readonly IMaGiamGiaService _maGiamGiaService;

        public ChiTietHoaDonsController(
            IChiTietHoaDonService chiTietHoaDonService, 
            IHoaDonService hoaDonService, 
            IPhongService phongService, 
            IDichVuService dichVuService, 
            IMaGiamGiaService maGiamGiaService)
        {
            _chiTietHoaDonService = chiTietHoaDonService;
            _hoaDonService = hoaDonService;
            _phongService = phongService;
            _dichVuService = dichVuService;
            _maGiamGiaService = maGiamGiaService;
        }

        public async Task<IActionResult> Index()
        {
            return View(await _chiTietHoaDonService.GetAllAsync());
        }

        public async Task<IActionResult> Details(string id)
        {
            if (id == null) return NotFound();

            var chiTietHoaDon = await _chiTietHoaDonService.GetByIdAsync(id);
            if (chiTietHoaDon == null) return NotFound();

            return View(chiTietHoaDon);
        }

        private async Task PrepareViewBags(ChiTietHoaDon chiTietHoaDon = null)
        {
            ViewData["MaHoaDon"] = new SelectList(await _hoaDonService.GetAllAsync(), "MaHoaDon", "MaHoaDon", chiTietHoaDon?.MaHoaDon);
            ViewData["MaPhong"] = new SelectList(await _phongService.GetAllAsync(), "MaPhong", "SoPhong", chiTietHoaDon?.MaPhong);
            ViewData["MaDv"] = new SelectList(await _dichVuService.GetAllAsync(), "MaDv", "TenDv", chiTietHoaDon?.MaDv);
            ViewData["MaGiamGia"] = new SelectList(await _maGiamGiaService.GetAllAsync(), "MaGiamGia", "TenMaGiamGia", chiTietHoaDon?.MaGiamGia);
        }

        public async Task<IActionResult> Create()
        {
            await PrepareViewBags();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("MaCthd,MaHoaDon,LoaiMuc,MaPhong,MaDv,MaGiamGia,NoiDung,NgayApDung,SoNguoi,SoLuong,DonGia,ThanhTien,TrangThai,GhiChu")] ChiTietHoaDon chiTietHoaDon)
        {
            ModelState.Remove("MaHoaDonNavigation");
            ModelState.Remove("MaPhongNavigation");
            ModelState.Remove("MaDvNavigation");
            ModelState.Remove("MaGiamGiaNavigation");

            if (string.IsNullOrWhiteSpace(chiTietHoaDon.MaPhong)) chiTietHoaDon.MaPhong = null;
            if (string.IsNullOrWhiteSpace(chiTietHoaDon.MaDv)) chiTietHoaDon.MaDv = null;
            if (string.IsNullOrWhiteSpace(chiTietHoaDon.MaGiamGia)) chiTietHoaDon.MaGiamGia = null;

            if (ModelState.IsValid)
            {
                if (await _chiTietHoaDonService.KiemTraTrungMaAsync(chiTietHoaDon.MaCthd))
                {
                    ModelState.AddModelError("MaCthd", "Mã chi tiết đã tồn tại.");
                }
                else
                {
                    await _chiTietHoaDonService.CreateAsync(chiTietHoaDon);
                    return RedirectToAction(nameof(Index));
                }
            }

            await PrepareViewBags(chiTietHoaDon);
            return View(chiTietHoaDon);
        }

        public async Task<IActionResult> Edit(string id)
        {
            if (id == null) return NotFound();

            var chiTietHoaDon = await _chiTietHoaDonService.GetByIdAsync(id);
            if (chiTietHoaDon == null) return NotFound();

            await PrepareViewBags(chiTietHoaDon);
            return View(chiTietHoaDon);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, [Bind("MaCthd,MaHoaDon,LoaiMuc,MaPhong,MaDv,MaGiamGia,NoiDung,NgayApDung,SoNguoi,SoLuong,DonGia,ThanhTien,TrangThai,GhiChu")] ChiTietHoaDon chiTietHoaDon)
        {
            if (id != chiTietHoaDon.MaCthd) return NotFound();

            ModelState.Remove("MaHoaDonNavigation");
            ModelState.Remove("MaPhongNavigation");
            ModelState.Remove("MaDvNavigation");
            ModelState.Remove("MaGiamGiaNavigation");

            if (string.IsNullOrWhiteSpace(chiTietHoaDon.MaPhong)) chiTietHoaDon.MaPhong = null;
            if (string.IsNullOrWhiteSpace(chiTietHoaDon.MaDv)) chiTietHoaDon.MaDv = null;
            if (string.IsNullOrWhiteSpace(chiTietHoaDon.MaGiamGia)) chiTietHoaDon.MaGiamGia = null;

            if (ModelState.IsValid)
            {
                await _chiTietHoaDonService.UpdateAsync(chiTietHoaDon);
                return RedirectToAction(nameof(Index));
            }

            await PrepareViewBags(chiTietHoaDon);
            return View(chiTietHoaDon);
        }

        public async Task<IActionResult> Delete(string id)
        {
            if (id == null) return NotFound();

            var chiTietHoaDon = await _chiTietHoaDonService.GetByIdAsync(id);
            if (chiTietHoaDon == null) return NotFound();

            return View(chiTietHoaDon);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            await _chiTietHoaDonService.DeleteAsync(id);
            return RedirectToAction(nameof(Index));
        }
    }
}
