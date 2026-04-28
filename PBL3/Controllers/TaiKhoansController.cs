using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using PBL3.Models;
using PBL3.Services.Interfaces;

namespace PBL3.Controllers
{
    public class TaiKhoansController : Controller
    {
        private readonly ITaiKhoanService _taiKhoanService;
        private readonly INhanVienService _nhanVienService;
        private readonly IVaiTroService _vaiTroService;

        public TaiKhoansController(ITaiKhoanService taiKhoanService, INhanVienService nhanVienService, IVaiTroService vaiTroService)
        {
            _taiKhoanService = taiKhoanService;
            _nhanVienService = nhanVienService;
            _vaiTroService = vaiTroService;
        }

        public async Task<IActionResult> Index()
        {
            var data = await _taiKhoanService.GetAllAsync();
            return View(data);
        }

        public async Task<IActionResult> Details(string id)
        {
            if (id == null) return NotFound();

            var taiKhoan = await _taiKhoanService.GetByIdAsync(id);
            if (taiKhoan == null) return NotFound();

            return View(taiKhoan);
        }

        public async Task<IActionResult> Create()
        {
            ViewData["MaNv"] = new SelectList(await _nhanVienService.GetAllAsync(), "MaNv", "HoTen");
            ViewData["MaVaiTro"] = new SelectList(await _vaiTroService.GetAllAsync(), "MaVaiTro", "TenVaiTro");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("MaTk,TenDangNhap,MatKhau,MaNv,MaVaiTro,TrangThai")] TaiKhoan taiKhoan)
        {
            ModelState.Remove("MaNvNavigation");
            ModelState.Remove("MaVaiTroNavigation");
            
            if (ModelState.IsValid)
            {
                if (await _taiKhoanService.KiemTraTrungMaAsync(taiKhoan.MaTk))
                {
                    ModelState.AddModelError("MaTk", "Mã tài khoản đã tồn tại.");
                }
                else if (await _taiKhoanService.KiemTraTrungTenDangNhapAsync(taiKhoan.TenDangNhap))
                {
                    ModelState.AddModelError("TenDangNhap", "Tên đăng nhập đã tồn tại.");
                }
                else if (await _taiKhoanService.KiemTraNVDaCoTaiKhoanAsync(taiKhoan.MaNv))
                {
                    ModelState.AddModelError("MaNv", "Nhân viên này đã có tài khoản.");
                }
                else
                {
                    await _taiKhoanService.CreateAsync(taiKhoan);
                    return RedirectToAction(nameof(Index));
                }
            }
            ViewData["MaNv"] = new SelectList(await _nhanVienService.GetAllAsync(), "MaNv", "HoTen", taiKhoan.MaNv);
            ViewData["MaVaiTro"] = new SelectList(await _vaiTroService.GetAllAsync(), "MaVaiTro", "TenVaiTro", taiKhoan.MaVaiTro);
            return View(taiKhoan);
        }

        public async Task<IActionResult> Edit(string id)
        {
            if (id == null) return NotFound();

            var taiKhoan = await _taiKhoanService.GetByIdAsync(id);
            if (taiKhoan == null) return NotFound();

            ViewData["MaNv"] = new SelectList(await _nhanVienService.GetAllAsync(), "MaNv", "HoTen", taiKhoan.MaNv);
            ViewData["MaVaiTro"] = new SelectList(await _vaiTroService.GetAllAsync(), "MaVaiTro", "TenVaiTro", taiKhoan.MaVaiTro);
            return View(taiKhoan);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, [Bind("MaTk,TenDangNhap,MatKhau,MaNv,MaVaiTro,TrangThai")] TaiKhoan taiKhoan)
        {
            if (id != taiKhoan.MaTk) return NotFound();

            ModelState.Remove("MaNvNavigation");
            ModelState.Remove("MaVaiTroNavigation");

            if (ModelState.IsValid)
            {
                if (await _taiKhoanService.KiemTraTrungTenDangNhapAsync(taiKhoan.TenDangNhap, taiKhoan.MaTk))
                {
                    ModelState.AddModelError("TenDangNhap", "Tên đăng nhập đã tồn tại.");
                }
                else if (await _taiKhoanService.KiemTraNVDaCoTaiKhoanAsync(taiKhoan.MaNv, taiKhoan.MaTk))
                {
                    ModelState.AddModelError("MaNv", "Nhân viên này đã có tài khoản.");
                }
                else
                {
                    await _taiKhoanService.UpdateAsync(taiKhoan);
                    return RedirectToAction(nameof(Index));
                }
            }
            ViewData["MaNv"] = new SelectList(await _nhanVienService.GetAllAsync(), "MaNv", "HoTen", taiKhoan.MaNv);
            ViewData["MaVaiTro"] = new SelectList(await _vaiTroService.GetAllAsync(), "MaVaiTro", "TenVaiTro", taiKhoan.MaVaiTro);
            return View(taiKhoan);
        }

        public async Task<IActionResult> Delete(string id)
        {
            if (id == null) return NotFound();

            var taiKhoan = await _taiKhoanService.GetByIdAsync(id);
            if (taiKhoan == null) return NotFound();

            return View(taiKhoan);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            await _taiKhoanService.DeleteAsync(id);
            return RedirectToAction(nameof(Index));
        }
    }
}
