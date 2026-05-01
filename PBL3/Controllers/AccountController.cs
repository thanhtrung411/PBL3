using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PBL3.Data;

namespace PBL3.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AccountController(ApplicationDbContext context)
        {
            _context = context;
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
                !IsPasswordValid(account.MatKhau, password))
            {
                ModelState.AddModelError(string.Empty, "Tài khoản hoặc mật khẩu không đúng.");
                return View();
            }

            var displayName = string.IsNullOrWhiteSpace(account.MaNvNavigation?.HoTen)
                ? account.TenDangNhap
                : account.MaNvNavigation.HoTen;
            var roleName = account.MaVaiTroNavigation?.TenVaiTro ?? account.MaVaiTro;

            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, account.MaTk.Trim()),
                new(ClaimTypes.Name, displayName),
                new(ClaimTypes.Role, roleName),
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

        private static bool IsActive(string? status)
        {
            return string.Equals(status?.Trim(), "Hoạt động", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsPasswordValid(string storedPassword, string submittedPassword)
        {
            return string.Equals(storedPassword, submittedPassword, StringComparison.Ordinal);
        }
    }
}
