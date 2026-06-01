using Microsoft.AspNetCore.Mvc;
using PBL3.Models;
using PBL3.Services.Interfaces;

namespace PBL3.Controllers.Admin
{
    [Area("Admin")]
    public class NhanViensController : Controller
    {
        private readonly INhanVienService _nhanVienService;
        private readonly IVaiTroService _vaiTroService;

        public NhanViensController(INhanVienService nhanVienService, IVaiTroService vaiTroService)
        {
            _nhanVienService = nhanVienService;
            _vaiTroService = vaiTroService;
        }

        // GET: NhanViens
        public async Task<IActionResult> Index()
        {
            var dsNhanVien = await _nhanVienService.GetAllAsync();
            return View(dsNhanVien);
        }

        // GET: NhanViens/Details/5
        public async Task<IActionResult> Details(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return NotFound();
            }

            var nhanVien = await _nhanVienService.GetByIdAsync(id);
            if (nhanVien == null)
            {
                return NotFound();
            }

            return View(nhanVien);
        }

        // GET: NhanViens/Create
        public async Task<IActionResult> Create()
        {
            var dsVaiTro = await _vaiTroService.GetAllAsync();
            ViewBag.ChucVuList = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(dsVaiTro, "TenVaiTro", "TenVaiTro");
            return View();
        }

        // POST: NhanViens/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("MaNv,HoTen,GioiTinh,NgaySinh,SoDienThoai,Email,DiaChi,ChucVu,TrangThai")] NhanVien nhanVien)
        {
            if (!ModelState.IsValid)
            {
                var dsVaiTro = await _vaiTroService.GetAllAsync();
                ViewBag.ChucVuList = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(dsVaiTro, "TenVaiTro", "TenVaiTro", nhanVien.ChucVu);
                return View(nhanVien);
            }

            if (await _nhanVienService.KiemTraTrungMaAsync(nhanVien.MaNv))
            {
                ModelState.AddModelError("MaNv", "Mã nhân viên đã tồn tại.");
                var dsVaiTro = await _vaiTroService.GetAllAsync();
                ViewBag.ChucVuList = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(dsVaiTro, "TenVaiTro", "TenVaiTro", nhanVien.ChucVu);
                return View(nhanVien);
            }

            if (!string.IsNullOrWhiteSpace(nhanVien.SoDienThoai) && await _nhanVienService.KiemTraTrungSoDienThoaiAsync(nhanVien.SoDienThoai))
            {
                ModelState.AddModelError("SoDienThoai", "Số điện thoại đã tồn tại.");
                var dsVaiTro = await _vaiTroService.GetAllAsync();
                ViewBag.ChucVuList = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(dsVaiTro, "TenVaiTro", "TenVaiTro", nhanVien.ChucVu);
                return View(nhanVien);
            }
            
            if (!string.IsNullOrWhiteSpace(nhanVien.Email) && await _nhanVienService.KiemTraTrungEmailAsync(nhanVien.Email))
            {
                ModelState.AddModelError("Email", "Email đã tồn tại.");
                var dsVaiTro = await _vaiTroService.GetAllAsync();
                ViewBag.ChucVuList = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(dsVaiTro, "TenVaiTro", "TenVaiTro", nhanVien.ChucVu);
                return View(nhanVien);
            }

            var createResult = await _nhanVienService.CreateAsync(nhanVien);
            if (createResult)
            {
                return RedirectToAction(nameof(Index));
            }

            ModelState.AddModelError(string.Empty, "Không thể tạo nhân viên. Vui lòng kiểm tra dữ liệu liên quan.");
            var createVaiTroList = await _vaiTroService.GetAllAsync();
            ViewBag.ChucVuList = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(createVaiTroList, "TenVaiTro", "TenVaiTro", nhanVien.ChucVu);
            return View(nhanVien);
        }

        // GET: NhanViens/Edit/5
        public async Task<IActionResult> Edit(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return NotFound();
            }

            var nhanVien = await _nhanVienService.GetByIdAsync(id);
            if (nhanVien == null)
            {
                return NotFound();
            }

            var dsVaiTro = await _vaiTroService.GetAllAsync();
            ViewBag.ChucVuList = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(dsVaiTro, "TenVaiTro", "TenVaiTro", nhanVien.ChucVu);
            return View(nhanVien);
        }

        // POST: NhanViens/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, [Bind("MaNv,HoTen,GioiTinh,NgaySinh,SoDienThoai,Email,DiaChi,ChucVu,TrangThai")] NhanVien nhanVien)
        {
            if (id != nhanVien.MaNv)
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                var dsVaiTro = await _vaiTroService.GetAllAsync();
                ViewBag.ChucVuList = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(dsVaiTro, "TenVaiTro", "TenVaiTro", nhanVien.ChucVu);
                return View(nhanVien);
            }

            if (!string.IsNullOrWhiteSpace(nhanVien.SoDienThoai) && await _nhanVienService.KiemTraTrungSoDienThoaiAsync(nhanVien.SoDienThoai, nhanVien.MaNv))
            {
                ModelState.AddModelError("SoDienThoai", "Số điện thoại đã tồn tại.");
                var dsVaiTro = await _vaiTroService.GetAllAsync();
                ViewBag.ChucVuList = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(dsVaiTro, "TenVaiTro", "TenVaiTro", nhanVien.ChucVu);
                return View(nhanVien);
            }
            
            if (!string.IsNullOrWhiteSpace(nhanVien.Email) && await _nhanVienService.KiemTraTrungEmailAsync(nhanVien.Email, nhanVien.MaNv))
            {
                ModelState.AddModelError("Email", "Email đã tồn tại.");
                var dsVaiTro = await _vaiTroService.GetAllAsync();
                ViewBag.ChucVuList = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(dsVaiTro, "TenVaiTro", "TenVaiTro", nhanVien.ChucVu);
                return View(nhanVien);
            }

            var updateResult = await _nhanVienService.UpdateAsync(nhanVien);
            if (updateResult)
            {
                return RedirectToAction(nameof(Index));
            }

            ModelState.AddModelError(string.Empty, "Không thể cập nhật nhân viên. Vui lòng kiểm tra dữ liệu liên quan.");
            var updateVaiTroList = await _vaiTroService.GetAllAsync();
            ViewBag.ChucVuList = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(updateVaiTroList, "TenVaiTro", "TenVaiTro", nhanVien.ChucVu);
            return View(nhanVien);
        }

        // GET: NhanViens/Delete/5
        public async Task<IActionResult> Delete(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return NotFound();
            }

            var nhanVien = await _nhanVienService.GetByIdAsync(id);
            if (nhanVien == null)
            {
                return NotFound();
            }

            return View(nhanVien);
        }

        // POST: NhanViens/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            var deleteResult = await _nhanVienService.DeleteAsync(id);
            if (!deleteResult)
            {
                TempData["Error"] = "Không thể xóa nhân viên vì dữ liệu đang được sử dụng hoặc không còn tồn tại.";
            }
            return RedirectToAction(nameof(Index));
        }
    }
}

