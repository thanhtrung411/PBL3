using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using PBL3.Models;
using PBL3.Services.Interfaces;

namespace PBL3.Controllers.Admin
{
    [Area("Admin")]
    public class TaiKhoansController : Controller
    {
        private readonly ITaiKhoanService _taiKhoanService;
        private readonly INhanVienService _nhanVienService;
        private readonly IVaiTroService _vaiTroService;
        private readonly IPasswordHasher<TaiKhoan> _passwordHasher;

        public TaiKhoansController(
            ITaiKhoanService taiKhoanService,
            INhanVienService nhanVienService,
            IVaiTroService vaiTroService,
            IPasswordHasher<TaiKhoan> passwordHasher)
        {
            _taiKhoanService = taiKhoanService;
            _nhanVienService = nhanVienService;
            _vaiTroService = vaiTroService;
            _passwordHasher = passwordHasher;
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
                    taiKhoan.MatKhau = _passwordHasher.HashPassword(taiKhoan, taiKhoan.MatKhau);
                    var createResult = await _taiKhoanService.CreateAsync(taiKhoan);
                    if (createResult)
                    {
                        return RedirectToAction(nameof(Index));
                    }

                    ModelState.AddModelError(string.Empty, "Không thể tạo tài khoản. Vui lòng kiểm tra dữ liệu liên quan.");
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

            taiKhoan.MatKhau = "";
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
            var submittedPassword = taiKhoan.MatKhau;
            var isChangingPassword = !string.IsNullOrWhiteSpace(submittedPassword);
            if (!isChangingPassword)
            {
                ModelState.Remove(nameof(TaiKhoan.MatKhau));
            }

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
                    var existingAccount = await _taiKhoanService.GetByIdAsync(taiKhoan.MaTk);
                    if (existingAccount == null)
                    {
                        return NotFound();
                    }

                    taiKhoan.MatKhau = isChangingPassword
                        ? _passwordHasher.HashPassword(taiKhoan, submittedPassword)
                        : existingAccount.MatKhau;

                    var updateResult = await _taiKhoanService.UpdateAsync(taiKhoan);
                    if (updateResult)
                    {
                        return RedirectToAction(nameof(Index));
                    }

                    ModelState.AddModelError(string.Empty, "Không thể cập nhật tài khoản. Vui lòng kiểm tra dữ liệu liên quan.");
                }
            }
            ViewData["MaNv"] = new SelectList(await _nhanVienService.GetAllAsync(), "MaNv", "HoTen", taiKhoan.MaNv);
            ViewData["MaVaiTro"] = new SelectList(await _vaiTroService.GetAllAsync(), "MaVaiTro", "TenVaiTro", taiKhoan.MaVaiTro);
            taiKhoan.MatKhau = "";
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
            var deleteResult = await _taiKhoanService.DeleteAsync(id);
            if (!deleteResult)
            {
                TempData["Error"] = "Không thể xóa tài khoản vì dữ liệu đang được sử dụng hoặc không còn tồn tại.";
            }
            return RedirectToAction(nameof(Index));
        }
    }
}

