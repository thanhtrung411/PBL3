using System.Security.Claims;
using System.Globalization;
using System.Text;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PBL3.Data;
using PBL3.Models;

namespace PBL3.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IPasswordHasher<TaiKhoan> _passwordHasher;

        public AccountController(
            ApplicationDbContext context,
            IPasswordHasher<TaiKhoan> passwordHasher)
        {
            _context = context;
            _passwordHasher = passwordHasher;
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Login(string? returnUrl = null)
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                if (IsReceptionist(User))
                {
                    return RedirectToAction("Index", "Receptionist");
                }

                if (Url.IsLocalUrl(returnUrl))
                {
                    return LocalRedirect(returnUrl);
                }

                return RedirectToAction("Index", "Home");
            }

            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(
            string email,
            string password,
            bool remember = false,
            string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;

            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                ModelState.AddModelError(string.Empty, "Vui lòng nhập tài khoản và mật khẩu.");
                return View();
            }

            var loginName = email.Trim();
            var account = await _context.TaiKhoans
                .Include(tk => tk.MaNvNavigation)
                .Include(tk => tk.MaVaiTroNavigation)
                .FirstOrDefaultAsync(tk =>
                    tk.TenDangNhap == loginName ||
                    tk.MaNvNavigation.Email == loginName);

            if (account == null ||
                !IsActive(account.TrangThai) ||
                !await IsPasswordValidAsync(account, password))
            {
                ModelState.AddModelError(string.Empty, "Tài khoản hoặc mật khẩu không đúng.");
                return View();
            }

            var displayName = string.IsNullOrWhiteSpace(account.MaNvNavigation?.HoTen)
                ? account.TenDangNhap
                : account.MaNvNavigation.HoTen;
            var roleName = (account.MaVaiTroNavigation?.TenVaiTro ?? account.MaVaiTro).Trim();
            var roleCode = account.MaVaiTro.Trim();
            var roleKey = GetCanonicalRoleKey(roleName, roleCode);

            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, account.MaTk.Trim()),
                new(ClaimTypes.Name, displayName),
                new(ClaimTypes.Role, roleKey),
                new("Username", account.TenDangNhap),
                new("EmployeeId", account.MaNv.Trim())
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);
            var properties = new AuthenticationProperties
            {
                IsPersistent = remember,
                ExpiresUtc = remember ? DateTimeOffset.UtcNow.AddDays(14) : null
            };

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                principal,
                properties);

            if (roleKey == "receptionist")
            {
                return RedirectToAction("Index", "Receptionist");
            }

            if (Url.IsLocalUrl(returnUrl))
            {
                return LocalRedirect(returnUrl);
            }

            return RedirectToAction("Index", "Home");
        }

        [Authorize]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction(nameof(Login));
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Settings()
        {
            var account = await GetCurrentAccountAsync();
            if (account == null)
            {
                return RedirectToAction(nameof(Login));
            }

            var employee = account.MaNvNavigation;
            return View(new AccountSettingsViewModel
            {
                AccountId = account.MaTk.Trim(),
                EmployeeId = account.MaNv.Trim(),
                Username = account.TenDangNhap.Trim(),
                FullName = employee.HoTen.Trim(),
                Gender = employee.GioiTinh?.Trim(),
                BirthDate = employee.NgaySinh,
                Phone = employee.SoDienThoai?.Trim(),
                Email = employee.Email?.Trim(),
                Address = employee.DiaChi?.Trim(),
                RoleName = account.MaVaiTroNavigation.TenVaiTro.Trim()
            });
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Settings(AccountSettingsViewModel model)
        {
            var account = await GetCurrentAccountAsync();
            if (account == null)
            {
                return RedirectToAction(nameof(Login));
            }

            model.AccountId = account.MaTk.Trim();
            model.EmployeeId = account.MaNv.Trim();
            model.RoleName = account.MaVaiTroNavigation.TenVaiTro.Trim();

            if (!string.IsNullOrWhiteSpace(model.NewPassword) ||
                !string.IsNullOrWhiteSpace(model.ConfirmPassword))
            {
                if (!string.Equals(model.NewPassword, model.ConfirmPassword, StringComparison.Ordinal))
                {
                    ModelState.AddModelError(nameof(model.ConfirmPassword), "Mật khẩu xác nhận không khớp.");
                }
            }

            var username = model.Username.Trim();
            var usernameExists = await _context.TaiKhoans
                .AnyAsync(x => x.TenDangNhap == username && x.MaTk != account.MaTk);
            if (usernameExists)
            {
                ModelState.AddModelError(nameof(model.Username), "Tên đăng nhập đã tồn tại.");
            }

            if (!string.IsNullOrWhiteSpace(model.Phone))
            {
                var phone = model.Phone.Trim();
                var phoneExists = await _context.NhanViens
                    .AnyAsync(x => x.SoDienThoai == phone && x.MaNv != account.MaNv);
                if (phoneExists)
                {
                    ModelState.AddModelError(nameof(model.Phone), "Số điện thoại đã tồn tại.");
                }
            }

            if (!string.IsNullOrWhiteSpace(model.Email))
            {
                var email = model.Email.Trim();
                var emailExists = await _context.NhanViens
                    .AnyAsync(x => x.Email == email && x.MaNv != account.MaNv);
                if (emailExists)
                {
                    ModelState.AddModelError(nameof(model.Email), "Email đã tồn tại.");
                }
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            account.TenDangNhap = username;
            if (!string.IsNullOrWhiteSpace(model.NewPassword))
            {
                account.MatKhau = _passwordHasher.HashPassword(account, model.NewPassword);
            }

            var employee = account.MaNvNavigation;
            employee.HoTen = model.FullName.Trim();
            employee.GioiTinh = model.Gender?.Trim();
            employee.NgaySinh = model.BirthDate;
            employee.SoDienThoai = model.Phone?.Trim();
            employee.Email = model.Email?.Trim();
            employee.DiaChi = model.Address?.Trim();

            await _context.SaveChangesAsync();
            await RefreshSignInAsync(account);
            TempData["SettingsSuccess"] = "Đã cập nhật thông tin tài khoản.";
            return RedirectToAction(nameof(Settings));
        }

        [AllowAnonymous]
        public IActionResult AccessDenied()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                if (IsReceptionist(User))
                {
                    return RedirectToAction("Index", "Receptionist");
                }

                if (IsAdmin(User))
                {
                    return RedirectToAction("Index", "Home");
                }
            }

            Response.StatusCode = StatusCodes.Status403Forbidden;
            return Content("Ban khong co quyen truy cap khu vuc quan tri.");
        }

        private static bool IsActive(string? status)
        {
            return string.Equals(status?.Trim(), "Hoạt động", StringComparison.OrdinalIgnoreCase);
        }

        private async Task<bool> IsPasswordValidAsync(TaiKhoan account, string submittedPassword)
        {
            var storedPassword = account.MatKhau;
            if (IsPasswordHash(storedPassword))
            {
                var verificationResult = _passwordHasher.VerifyHashedPassword(
                    account,
                    storedPassword,
                    submittedPassword);

                if (verificationResult == PasswordVerificationResult.SuccessRehashNeeded)
                {
                    account.MatKhau = _passwordHasher.HashPassword(account, submittedPassword);
                    await _context.SaveChangesAsync();
                }

                return verificationResult != PasswordVerificationResult.Failed;
            }

            if (!string.Equals(storedPassword, submittedPassword, StringComparison.Ordinal))
            {
                return false;
            }

            account.MatKhau = _passwordHasher.HashPassword(account, submittedPassword);
            await _context.SaveChangesAsync();
            return true;
        }

        private async Task<TaiKhoan?> GetCurrentAccountAsync()
        {
            var accountId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(accountId))
            {
                return null;
            }

            return await _context.TaiKhoans
                .Include(x => x.MaNvNavigation)
                .Include(x => x.MaVaiTroNavigation)
                .FirstOrDefaultAsync(x => x.MaTk == accountId);
        }

        private async Task RefreshSignInAsync(TaiKhoan account)
        {
            var displayName = string.IsNullOrWhiteSpace(account.MaNvNavigation?.HoTen)
                ? account.TenDangNhap
                : account.MaNvNavigation.HoTen;
            var roleName = (account.MaVaiTroNavigation?.TenVaiTro ?? account.MaVaiTro).Trim();
            var roleCode = account.MaVaiTro.Trim();
            var roleKey = GetCanonicalRoleKey(roleName, roleCode);

            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, account.MaTk.Trim()),
                new(ClaimTypes.Name, displayName),
                new(ClaimTypes.Role, roleKey),
                new("Username", account.TenDangNhap.Trim()),
                new("EmployeeId", account.MaNv.Trim())
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);
            var authenticateResult = await HttpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                principal,
                authenticateResult.Properties ?? new AuthenticationProperties());
        }

        private static bool IsPasswordHash(string? password)
        {
            return !string.IsNullOrWhiteSpace(password) &&
                   password.StartsWith("AQAAAA", StringComparison.Ordinal);
        }

        private static bool IsReceptionist(ClaimsPrincipal user)
        {
            return user.FindAll(ClaimTypes.Role)
                .Any(claim => IsReceptionistRole(claim.Value));
        }

        private static bool IsAdmin(ClaimsPrincipal user)
        {
            return user.FindAll(ClaimTypes.Role)
                .Any(claim => GetCanonicalRoleKey(claim.Value) == "admin");
        }

        private static bool IsReceptionistRole(params string?[] roles)
        {
            return roles
                .Any(role => GetCanonicalRoleKey(role) == "receptionist");
        }

        private static string GetCanonicalRoleKey(params string?[] roles)
        {
            var normalizedRoles = roles.Select(NormalizeRoleKey).ToHashSet(StringComparer.OrdinalIgnoreCase);
            if (normalizedRoles.Contains("admin") || normalizedRoles.Contains("vt001"))
            {
                return "admin";
            }

            if (normalizedRoles.Contains("receptionist") ||
                normalizedRoles.Contains("vt002") ||
                normalizedRoles.Contains("le tan"))
            {
                return "receptionist";
            }

            return string.Empty;
        }

        private static string NormalizeRoleKey(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            var normalized = value.Trim().Normalize(NormalizationForm.FormD);
            var builder = new StringBuilder(normalized.Length);
            foreach (var character in normalized)
            {
                var category = CharUnicodeInfo.GetUnicodeCategory(character);
                if (category != UnicodeCategory.NonSpacingMark)
                {
                    builder.Append(character);
                }
            }

            return builder
                .ToString()
                .Normalize(NormalizationForm.FormC)
                .ToLowerInvariant();
        }
    }
}
