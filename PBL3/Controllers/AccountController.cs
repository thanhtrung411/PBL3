using System.Security.Claims;
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
            if (User.Identity?.IsAuthenticated == true && Url.IsLocalUrl(returnUrl))
            {
                return LocalRedirect(returnUrl);
            }

            if (User.Identity?.IsAuthenticated == true)
            {
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

            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, account.MaTk.Trim()),
                new(ClaimTypes.Name, displayName),
                new(ClaimTypes.Role, roleName),
                new(ClaimTypes.Role, roleCode),
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

        [AllowAnonymous]
        public IActionResult AccessDenied()
        {
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

        private static bool IsPasswordHash(string? password)
        {
            return !string.IsNullOrWhiteSpace(password) &&
                   password.StartsWith("AQAAAA", StringComparison.Ordinal);
        }
    }
}
