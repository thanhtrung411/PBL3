using System.Globalization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PBL3.Data;
using PBL3.Models;
using PBL3.Services;
using PBL3.Services.Interfaces;

namespace PBL3.Controllers;

public class StaffController : Controller
{
    private static readonly CultureInfo ViCulture = CultureInfo.GetCultureInfo("vi-VN");

    private readonly ApplicationDbContext _context;
    private readonly INhanVienService _nhanVienService;
    private readonly ITaiKhoanService _taiKhoanService;
    private readonly IPasswordHasher<TaiKhoan> _passwordHasher;

    public StaffController(
        ApplicationDbContext context,
        INhanVienService nhanVienService,
        ITaiKhoanService taiKhoanService,
        IPasswordHasher<TaiKhoan> passwordHasher)
    {
        _context = context;
        _nhanVienService = nhanVienService;
        _taiKhoanService = taiKhoanService;
        _passwordHasher = passwordHasher;
    }

    public async Task<IActionResult> Index()
    {
        var employees = await _context.NhanViens
            .AsNoTracking()
            .Include(x => x.TaiKhoan)
                .ThenInclude(x => x!.MaVaiTroNavigation)
            .OrderBy(x => x.HoTen)
            .ToListAsync();

        var bookingCounts = await _context.DatPhongs
            .AsNoTracking()
            .GroupBy(x => x.MaNv)
            .Select(x => new
            {
                EmployeeId = x.Key,
                Count = x.Count()
            })
            .ToListAsync();
        var bookingCountByEmployee = bookingCounts.ToDictionary(
            x => x.EmployeeId.Trim(),
            x => x.Count,
            StringComparer.OrdinalIgnoreCase);

        var positions = await _context.VaiTros
            .AsNoTracking()
            .Select(x => x.TenVaiTro)
            .Union(_context.NhanViens.AsNoTracking().Where(x => x.ChucVu != null).Select(x => x.ChucVu!))
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct()
            .OrderBy(x => x)
            .ToListAsync();
        var roles = await _context.VaiTros
            .AsNoTracking()
            .OrderBy(x => x.TenVaiTro)
            .Select(x => new AdminStaffRoleOptionViewModel
            {
                RoleId = x.MaVaiTro.Trim(),
                RoleName = x.TenVaiTro.Trim()
            })
            .ToListAsync();

        var viewModel = new AdminStaffManagementViewModel
        {
            Positions = positions,
            Roles = roles,
            Staff = employees.Select(employee =>
            {
                var employeeId = employee.MaNv.Trim();
                bookingCountByEmployee.TryGetValue(employeeId, out var bookingCount);

                return new AdminStaffManagementItemViewModel
                {
                    EmployeeId = employeeId,
                    FullName = employee.HoTen.Trim(),
                    Gender = employee.GioiTinh?.Trim() ?? string.Empty,
                    BirthDateLabel = employee.NgaySinh?.ToString("dd/MM/yyyy", ViCulture) ?? "Chưa có",
                    BirthDateInput = employee.NgaySinh?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? string.Empty,
                    Phone = employee.SoDienThoai?.Trim() ?? string.Empty,
                    Email = employee.Email?.Trim() ?? string.Empty,
                    Address = employee.DiaChi?.Trim() ?? string.Empty,
                    Position = employee.ChucVu?.Trim() ?? string.Empty,
                    Status = employee.TrangThai.Trim(),
                    HasAccount = employee.TaiKhoan != null,
                    AccountId = employee.TaiKhoan?.MaTk.Trim() ?? string.Empty,
                    AccountName = employee.TaiKhoan?.TenDangNhap.Trim() ?? string.Empty,
                    AccountRole = employee.TaiKhoan?.MaVaiTroNavigation.TenVaiTro.Trim() ?? string.Empty,
                    AccountStatus = employee.TaiKhoan?.TrangThai.Trim() ?? string.Empty,
                    BookingCount = bookingCount,
                    Initials = GetInitials(employee.HoTen)
                };
            }).ToList()
        };

        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(AdminStaffCreateRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.HoTen) ||
            string.IsNullOrWhiteSpace(request.TenDangNhap) ||
            string.IsNullOrWhiteSpace(request.MatKhau) ||
            string.IsNullOrWhiteSpace(request.MaVaiTro))
        {
            TempData["StaffError"] = "Vui lòng nhập đầy đủ thông tin nhân viên và tài khoản.";
            return RedirectToAction(nameof(Index));
        }

        var employeeId = await GenerateEmployeeIdAsync();
        var username = request.TenDangNhap.Trim();
        var roleId = request.MaVaiTro.Trim();
        if (string.IsNullOrWhiteSpace(employeeId))
        {
            TempData["StaffError"] = "Không thể sinh mã nhân viên mới.";
            return RedirectToAction(nameof(Index));
        }

        if (!string.IsNullOrWhiteSpace(request.SoDienThoai) &&
            await _nhanVienService.KiemTraTrungSoDienThoaiAsync(request.SoDienThoai.Trim()))
        {
            TempData["StaffError"] = "Số điện thoại nhân viên đã tồn tại.";
            return RedirectToAction(nameof(Index));
        }

        if (!string.IsNullOrWhiteSpace(request.Email) &&
            await _nhanVienService.KiemTraTrungEmailAsync(request.Email.Trim()))
        {
            TempData["StaffError"] = "Email nhân viên đã tồn tại.";
            return RedirectToAction(nameof(Index));
        }

        if (await _taiKhoanService.KiemTraTrungTenDangNhapAsync(username))
        {
            TempData["StaffError"] = "Tên đăng nhập đã tồn tại.";
            return RedirectToAction(nameof(Index));
        }

        if (!await _context.VaiTros.AsNoTracking().AnyAsync(x => x.MaVaiTro == roleId))
        {
            TempData["StaffError"] = "Vai trò tài khoản không hợp lệ.";
            return RedirectToAction(nameof(Index));
        }

        var accountId = await GenerateAccountIdAsync();
        if (string.IsNullOrWhiteSpace(accountId))
        {
            TempData["StaffError"] = "Không thể sinh mã tài khoản mới.";
            return RedirectToAction(nameof(Index));
        }

        await using var transaction = await _context.Database.BeginTransactionAsync();
        var employee = new NhanVien
        {
            MaNv = employeeId,
            HoTen = request.HoTen,
            GioiTinh = request.GioiTinh,
            NgaySinh = request.NgaySinh,
            SoDienThoai = request.SoDienThoai,
            Email = request.Email,
            DiaChi = request.DiaChi,
            ChucVu = request.ChucVu,
            TrangThai = string.IsNullOrWhiteSpace(request.TrangThai)
                ? DomainValues.NhanVienTrangThai.DangLam
                : request.TrangThai
        };
        var account = new TaiKhoan
        {
            MaTk = accountId,
            TenDangNhap = username,
            MatKhau = request.MatKhau,
            MaNv = employeeId,
            MaVaiTro = roleId,
            TrangThai = "Hoạt động"
        };
        account.MatKhau = _passwordHasher.HashPassword(account, account.MatKhau);

        _context.NhanViens.Add(employee);
        _context.TaiKhoans.Add(account);
        await _context.SaveChangesAsync();
        await transaction.CommitAsync();

        TempData["StaffSuccess"] = $"Đã thêm nhân viên mới và tạo tài khoản {username}.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(AdminStaffUpdateRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.MaNv) || string.IsNullOrWhiteSpace(request.HoTen))
        {
            TempData["StaffError"] = "Vui lòng nhập đầy đủ mã và họ tên nhân viên.";
            return RedirectToAction(nameof(Index));
        }

        var employee = await _nhanVienService.GetByIdAsync(request.MaNv.Trim());
        if (employee == null)
        {
            TempData["StaffError"] = "Không tìm thấy nhân viên cần cập nhật.";
            return RedirectToAction(nameof(Index));
        }

        employee.HoTen = request.HoTen;
        employee.GioiTinh = request.GioiTinh;
        employee.NgaySinh = request.NgaySinh;
        employee.SoDienThoai = request.SoDienThoai;
        employee.Email = request.Email;
        employee.DiaChi = request.DiaChi;
        employee.ChucVu = request.ChucVu;
        employee.TrangThai = string.IsNullOrWhiteSpace(request.TrangThai)
            ? DomainValues.NhanVienTrangThai.DangLam
            : request.TrangThai;

        var updated = await _nhanVienService.UpdateAsync(employee);
        TempData[updated ? "StaffSuccess" : "StaffError"] = updated
            ? "Đã cập nhật thông tin nhân viên."
            : "Không thể cập nhật nhân viên. Email hoặc số điện thoại có thể đã tồn tại.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            TempData["StaffError"] = "Thiếu mã nhân viên.";
            return RedirectToAction(nameof(Index));
        }

        var deleted = await _nhanVienService.DeleteAsync(id.Trim());
        TempData[deleted ? "StaffSuccess" : "StaffError"] = deleted
            ? "Đã xóa nhân viên."
            : "Không thể xóa nhân viên vì đã phát sinh dữ liệu liên quan.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> LockAccount(string accountId)
    {
        var account = await UpdateAccountStatusAsync(accountId, "Bị khóa");
        var updated = account != null;
        var message = updated
            ? "Đã khóa tài khoản nhân viên."
            : "Không tìm thấy tài khoản cần khóa.";
        if (IsAjaxRequest())
        {
            return Json(new
            {
                success = updated,
                message,
                accountId = account?.MaTk.Trim() ?? accountId,
                accountStatus = account?.TrangThai.Trim() ?? string.Empty
            });
        }

        TempData[updated ? "StaffSuccess" : "StaffError"] = message;
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RestoreAccount(string accountId)
    {
        var account = await UpdateAccountStatusAsync(accountId, "Hoạt động");
        var updated = account != null;
        var message = updated
            ? "Đã khôi phục tài khoản nhân viên."
            : "Không tìm thấy tài khoản cần khôi phục.";
        if (IsAjaxRequest())
        {
            return Json(new
            {
                success = updated,
                message,
                accountId = account?.MaTk.Trim() ?? accountId,
                accountStatus = account?.TrangThai.Trim() ?? string.Empty
            });
        }

        TempData[updated ? "StaffSuccess" : "StaffError"] = message;
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(string accountId)
    {
        if (string.IsNullOrWhiteSpace(accountId))
        {
            TempData["StaffError"] = "Thiếu mã tài khoản cần đặt lại mật khẩu.";
            return RedirectToAction(nameof(Index));
        }

        var account = await _context.TaiKhoans.FirstOrDefaultAsync(x => x.MaTk == accountId.Trim());
        if (account == null)
        {
            TempData["StaffError"] = "Không tìm thấy tài khoản cần đặt lại mật khẩu.";
            return RedirectToAction(nameof(Index));
        }

        account.MatKhau = _passwordHasher.HashPassword(account, "123456");
        await _context.SaveChangesAsync();
        TempData["StaffSuccess"] = $"Đã đặt lại mật khẩu tài khoản {account.TenDangNhap.Trim()} thành 123456.";
        return RedirectToAction(nameof(Index));
    }

    private async Task<TaiKhoan?> UpdateAccountStatusAsync(string accountId, string status)
    {
        if (string.IsNullOrWhiteSpace(accountId))
        {
            return null;
        }

        var account = await _context.TaiKhoans.FirstOrDefaultAsync(x => x.MaTk == accountId.Trim());
        if (account == null)
        {
            return null;
        }

        account.TrangThai = status;
        await _context.SaveChangesAsync();
        return account;
    }

    private bool IsAjaxRequest()
    {
        return string.Equals(
            Request.Headers["X-Requested-With"],
            "XMLHttpRequest",
            StringComparison.OrdinalIgnoreCase);
    }

    private async Task<string?> GenerateAccountIdAsync()
    {
        try
        {
            var sequenceCode = await CodeGenerator.GenerateFromSequenceAsync(
                _context,
                "dbo.Seq_TaiKhoan",
                "TK",
                8);
            if (!string.IsNullOrWhiteSpace(sequenceCode))
            {
                return sequenceCode;
            }
        }
        catch
        {
            // Some database versions do not include Seq_TaiKhoan; fall back to a table scan.
        }

        var existingIds = await _context.TaiKhoans
            .AsNoTracking()
            .Select(x => x.MaTk)
            .ToListAsync();
        var used = existingIds
            .Select(x => x.Trim())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        for (var i = 1; i <= 99_999_999; i++)
        {
            var candidate = "TK" + i.ToString("D8", CultureInfo.InvariantCulture);
            if (!used.Contains(candidate))
            {
                return candidate;
            }
        }

        return null;
    }

    private async Task<string?> GenerateEmployeeIdAsync()
    {
        try
        {
            var sequenceCode = await CodeGenerator.GenerateFromSequenceAsync(
                _context,
                "dbo.Seq_NhanVien",
                "NV",
                8);
            if (!string.IsNullOrWhiteSpace(sequenceCode) &&
                !await _context.NhanViens.AsNoTracking().AnyAsync(x => x.MaNv == sequenceCode))
            {
                return sequenceCode;
            }
        }
        catch
        {
            // Some database versions do not include Seq_NhanVien; fall back to a table scan.
        }

        var existingIds = await _context.NhanViens
            .AsNoTracking()
            .Select(x => x.MaNv)
            .ToListAsync();
        var used = existingIds
            .Select(x => x.Trim())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        for (var i = 1; i <= 99_999_999; i++)
        {
            var candidate = "NV" + i.ToString("D8", CultureInfo.InvariantCulture);
            if (!used.Contains(candidate))
            {
                return candidate;
            }
        }

        return null;
    }

    private static string GetInitials(string value)
    {
        var words = value.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (words.Length == 0)
        {
            return "NV";
        }

        if (words.Length == 1)
        {
            return words[0][..Math.Min(2, words[0].Length)].ToUpperInvariant();
        }

        return $"{words[^2][0]}{words[^1][0]}".ToUpperInvariant();
    }
}
