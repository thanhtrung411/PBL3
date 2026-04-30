using Microsoft.AspNetCore.Mvc;
using PBL3.Models;
using PBL3.Services.Interfaces;

namespace PBL3.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class KhachHangsController : Controller
    {
        private readonly IKhachHangService _khachHangService;

        public KhachHangsController(IKhachHangService khachHangService)
        {
            _khachHangService = khachHangService;
        }

        private Microsoft.AspNetCore.Mvc.Rendering.SelectList GetQuocTichSelectList(string? selectedValue = null)
        {
            var dsQuocTich = new List<string> { 
                "Việt Nam", "Mỹ", "Anh", "Pháp", "Đức", "Nhật Bản", 
                "Hàn Quốc", "Trung Quốc", "Đài Loan", "Úc", "Canada", 
                "Singapore", "Thái Lan", "Malaysia", "Khác" 
            };
            return new Microsoft.AspNetCore.Mvc.Rendering.SelectList(dsQuocTich, selectedValue);
        }

        public async Task<IActionResult> Index()
        {
            return View(await _khachHangService.GetAllAsync());
        }

        public async Task<IActionResult> Details(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return NotFound();
            var khachHang = await _khachHangService.GetByIdAsync(id);
            if (khachHang == null) return NotFound();
            return View(khachHang);
        }

        public IActionResult Create()
        {
            ViewBag.QuocTichList = GetQuocTichSelectList();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("MaKh,HoTen,GioiTinh,NgaySinh,Cccd,SoDienThoai,Email,DiaChi,QuocTich")] KhachHang khachHang)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.QuocTichList = GetQuocTichSelectList(khachHang.QuocTich);
                return View(khachHang);
            }

            if (await _khachHangService.KiemTraTrungMaAsync(khachHang.MaKh))
            {
                ModelState.AddModelError("MaKh", "Mã khách hàng đã tồn tại.");
                ViewBag.QuocTichList = GetQuocTichSelectList(khachHang.QuocTich);
                return View(khachHang);
            }

            if (!string.IsNullOrWhiteSpace(khachHang.Cccd) && await _khachHangService.KiemTraTrungCccdAsync(khachHang.Cccd))
            {
                ModelState.AddModelError("Cccd", "CCCD đã tồn tại.");
                ViewBag.QuocTichList = GetQuocTichSelectList(khachHang.QuocTich);
                return View(khachHang);
            }

            var createResult = await _khachHangService.CreateAsync(khachHang);
            if (createResult)
            {
                return RedirectToAction(nameof(Index));
            }

            ModelState.AddModelError(string.Empty, "Không thể tạo khách hàng. Vui lòng kiểm tra dữ liệu liên quan.");
            ViewBag.QuocTichList = GetQuocTichSelectList(khachHang.QuocTich);
            return View(khachHang);
        }

        public async Task<IActionResult> Edit(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return NotFound();
            var khachHang = await _khachHangService.GetByIdAsync(id);
            if (khachHang == null) return NotFound();
            ViewBag.QuocTichList = GetQuocTichSelectList(khachHang.QuocTich);
            return View(khachHang);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, [Bind("MaKh,HoTen,GioiTinh,NgaySinh,Cccd,SoDienThoai,Email,DiaChi,QuocTich")] KhachHang khachHang)
        {
            if (id != khachHang.MaKh) return NotFound();

            if (!ModelState.IsValid)
            {
                ViewBag.QuocTichList = GetQuocTichSelectList(khachHang.QuocTich);
                return View(khachHang);
            }

            if (!string.IsNullOrWhiteSpace(khachHang.Cccd) && await _khachHangService.KiemTraTrungCccdAsync(khachHang.Cccd, khachHang.MaKh))
            {
                ModelState.AddModelError("Cccd", "CCCD đã tồn tại.");
                ViewBag.QuocTichList = GetQuocTichSelectList(khachHang.QuocTich);
                return View(khachHang);
            }

            var updateResult = await _khachHangService.UpdateAsync(khachHang);
            if (updateResult)
            {
                return RedirectToAction(nameof(Index));
            }

            ModelState.AddModelError(string.Empty, "Không thể cập nhật khách hàng. Vui lòng kiểm tra dữ liệu liên quan.");
            ViewBag.QuocTichList = GetQuocTichSelectList(khachHang.QuocTich);
            return View(khachHang);
        }

        public async Task<IActionResult> Delete(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return NotFound();
            var khachHang = await _khachHangService.GetByIdAsync(id);
            if (khachHang == null) return NotFound();
            return View(khachHang);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            var deleteResult = await _khachHangService.DeleteAsync(id);
            if (!deleteResult)
            {
                TempData["Error"] = "Không thể xóa khách hàng vì dữ liệu đang được sử dụng hoặc không còn tồn tại.";
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> GetByCccd(string cccd)
        {
            if (string.IsNullOrWhiteSpace(cccd)) return Json(new { success = false });
            
            var khachHang = await _khachHangService.GetByCccdAsync(cccd);
            if (khachHang != null)
            {
                return Json(new { 
                    success = true, 
                    hoTen = khachHang.HoTen, 
                    soDienThoai = khachHang.SoDienThoai, 
                    maKh = khachHang.MaKh 
                });
            }
            return Json(new { success = false });
        }
    }
}

