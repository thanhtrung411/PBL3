using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PBL3.Data;
using PBL3.Models;
using PBL3.Services.Interfaces;

namespace PBL3.Controllers
{
    public class ServiceController : Controller
    {
        private static readonly string[] FixedServiceCategories =
        {
            "Ăn uống",
            "Giải trí",
            "Tiện ích",
            "Vận chuyển"
        };

        private readonly ApplicationDbContext _context;
        private readonly IDichVuService _dichVuService;

        public ServiceController(ApplicationDbContext context, IDichVuService dichVuService)
        {
            _context = context;
            _dichVuService = dichVuService;
        }

        public async Task<IActionResult> Index()
        {
            var services = await _context.DichVus
                .AsNoTracking()
                .OrderBy(x => x.LoaiDichVu)
                .ThenBy(x => x.TenDv)
                .ToListAsync();

            var monthStart = new DateOnly(DateTime.Today.Year, DateTime.Today.Month, 1);
            var monthEnd = monthStart.AddMonths(1);
            var serviceUsage = await _context.ChiTietHoaDons
                .AsNoTracking()
                .Where(x => x.LoaiMuc == DomainValues.ChiTietHoaDonLoaiMuc.DichVu &&
                            x.TrangThai == DomainValues.ChiTietHoaDonTrangThai.HieuLuc &&
                            x.MaDv != null &&
                            x.NgayApDung >= monthStart &&
                            x.NgayApDung < monthEnd)
                .GroupBy(x => x.MaDv!)
                .Select(x => new
                {
                    ServiceId = x.Key,
                    Quantity = x.Sum(item => item.SoLuong),
                    Revenue = x.Sum(item => item.ThanhTien)
                })
                .ToListAsync();
            var usageByService = serviceUsage.ToDictionary(x => x.ServiceId, StringComparer.OrdinalIgnoreCase);

            var viewModel = new AdminServiceManagementViewModel
            {
                Categories = FixedServiceCategories.ToList(),
                MonthlyUsageCount = serviceUsage.Sum(x => x.Quantity),
                MonthlyRevenue = serviceUsage.Sum(x => x.Revenue),
                Services = services.Select(service =>
                {
                    usageByService.TryGetValue(service.MaDv, out var usage);
                    var revenue = usage?.Revenue ?? 0;
                    return new AdminServiceManagementItemViewModel
                    {
                        ServiceId = service.MaDv.Trim(),
                        ServiceName = service.TenDv.Trim(),
                        Category = NormalizeCategory(service.LoaiDichVu),
                        Unit = service.DonViTinh.Trim(),
                        Price = service.DonGia,
                        PriceLabel = service.DonGia <= 0
                            ? "Miễn phí"
                            : $"{service.DonGia.ToString("N0", CultureInfo.GetCultureInfo("vi-VN"))} đ",
                        Status = service.TrangThai.Trim(),
                        IsActive = IsActiveStatus(service.TrangThai),
                        Description = string.IsNullOrWhiteSpace(service.GhiChu)
                            ? "Chưa có mô tả cho dịch vụ này."
                            : service.GhiChu.Trim(),
                        IconClass = GetCategoryIcon(service.LoaiDichVu),
                        MonthlyUsageCount = usage?.Quantity ?? 0,
                        MonthlyRevenueLabel = FormatCurrency(revenue)
                    };
                }).ToList()
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(DichVu model)
        {
            NormalizeService(model);
            if (!IsValidService(model, out var message))
            {
                TempData["ServiceError"] = message;
                return RedirectToAction(nameof(Index));
            }

            var created = await _dichVuService.CreateAsync(model);
            TempData[created ? "ServiceSuccess" : "ServiceError"] = created
                ? "Đã thêm dịch vụ mới."
                : "Không thể thêm dịch vụ. Tên dịch vụ có thể đã tồn tại.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(DichVu model)
        {
            if (string.IsNullOrWhiteSpace(model.MaDv))
            {
                TempData["ServiceError"] = "Thiếu mã dịch vụ.";
                return RedirectToAction(nameof(Index));
            }

            NormalizeService(model);
            if (!IsValidService(model, out var message))
            {
                TempData["ServiceError"] = message;
                return RedirectToAction(nameof(Index));
            }

            var updated = await _dichVuService.UpdateAsync(model);
            TempData[updated ? "ServiceSuccess" : "ServiceError"] = updated
                ? "Đã cập nhật dịch vụ."
                : "Không thể cập nhật dịch vụ. Tên dịch vụ có thể đã tồn tại.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                TempData["ServiceError"] = "Thiếu mã dịch vụ.";
                return RedirectToAction(nameof(Index));
            }

            var deleted = await _dichVuService.DeleteAsync(id.Trim());
            TempData[deleted ? "ServiceSuccess" : "ServiceError"] = deleted
                ? "Đã xóa dịch vụ."
                : "Không thể xóa dịch vụ vì đã phát sinh dữ liệu sử dụng.";
            return RedirectToAction(nameof(Index));
        }

        private static void NormalizeService(DichVu model)
        {
            model.MaDv = model.MaDv?.Trim() ?? string.Empty;
            model.TenDv = model.TenDv?.Trim() ?? string.Empty;
            model.DonViTinh = model.DonViTinh?.Trim() ?? string.Empty;
            model.TrangThai = string.IsNullOrWhiteSpace(model.TrangThai)
                ? "Hoạt động"
                : model.TrangThai.Trim();
            model.GhiChu = model.GhiChu?.Trim();
            model.LoaiDichVu = NormalizeCategory(model.LoaiDichVu);
        }

        private static bool IsValidService(DichVu model, out string message)
        {
            if (string.IsNullOrWhiteSpace(model.TenDv))
            {
                message = "Vui lòng nhập tên dịch vụ.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(model.DonViTinh))
            {
                message = "Vui lòng nhập đơn vị tính.";
                return false;
            }

            if (model.DonGia < 0)
            {
                message = "Đơn giá không được âm.";
                return false;
            }

            message = string.Empty;
            return true;
        }

        private static string NormalizeCategory(string? category)
        {
            if (string.IsNullOrWhiteSpace(category))
            {
                return "Tiện ích";
            }

            var normalized = category.Trim();
            return FixedServiceCategories.FirstOrDefault(
                       x => string.Equals(x, normalized, StringComparison.OrdinalIgnoreCase)) ??
                   "Tiện ích";
        }

        private static bool IsActiveStatus(string? status)
        {
            return !string.IsNullOrWhiteSpace(status) &&
                   (status.Contains("Hoạt", StringComparison.OrdinalIgnoreCase) ||
                    status.Equals("active", StringComparison.OrdinalIgnoreCase) ||
                    status.Equals("Đang hoạt động", StringComparison.OrdinalIgnoreCase));
        }

        private static string GetCategoryIcon(string? category)
        {
            var value = NormalizeCategory(category).ToLowerInvariant();
            if (value.Contains("ăn") || value.Contains("uong") || value.Contains("uống"))
            {
                return "bi-cup-hot";
            }

            if (value.Contains("giải") || value.Contains("giai"))
            {
                return "bi-stars";
            }

            if (value.Contains("vận") || value.Contains("van") || value.Contains("chuyển"))
            {
                return "bi-car-front";
            }

            if (value.Contains("tiện") || value.Contains("tien"))
            {
                return "bi-basket";
            }

            return "bi-gear";
        }

        private static string FormatCurrency(decimal value)
        {
            return $"{value.ToString("N0", CultureInfo.GetCultureInfo("vi-VN"))} đ";
        }
    }
}
