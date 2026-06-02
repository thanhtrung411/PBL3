using System.Globalization;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PBL3.Data;
using PBL3.Models;

namespace PBL3.Controllers
{
    public class PromotionController : Controller
    {
        private readonly ApplicationDbContext _context;

        public PromotionController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var today = DateOnly.FromDateTime(DateTime.Today);
            var promotions = await _context.MaGiamGia
                .AsNoTracking()
                .OrderByDescending(x => x.TuNgay <= today && x.DenNgay >= today)
                .ThenByDescending(x => x.TuNgay)
                .ThenBy(x => x.CodeGiamGia)
                .ToListAsync();

            var invoicePromotionUsage = await _context.HoaDons
                .AsNoTracking()
                .Where(x => x.MaGiamGiaPhong != null &&
                            x.TienGiamGiaPhong > 0 &&
                            x.TrangThai != DomainValues.HoaDonTrangThai.DaHuy)
                .Select(x => new
                {
                    PromotionId = x.MaGiamGiaPhong!,
                    Amount = x.TienGiamGiaPhong
                })
                .ToListAsync();
            var usageByPromotion = invoicePromotionUsage
                .GroupBy(x => x.PromotionId.Trim(), StringComparer.OrdinalIgnoreCase)
                .Select(x => new
                {
                    PromotionId = x.Key,
                    UsedCount = x.Count(),
                    Amount = x.Sum(item => item.Amount)
                })
                .ToList();
            var usageMap = usageByPromotion.ToDictionary(x => x.PromotionId, StringComparer.OrdinalIgnoreCase);

            var viewModel = new AdminPromotionManagementViewModel
            {
                TotalDiscountAmount = usageByPromotion.Sum(x => x.Amount),
                Promotions = promotions.Select(x =>
                {
                    usageMap.TryGetValue(x.MaGiamGia.Trim(), out var usage);
                    return BuildPromotionItem(x, today, usage?.UsedCount ?? 0);
                }).ToList()
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(MaGiamGium model)
        {
            NormalizePromotion(model);
            if (!ValidatePromotion(model, out var message))
            {
                TempData["PromotionError"] = message;
                return RedirectToAction(nameof(Index));
            }

            if (await _context.MaGiamGia.AnyAsync(x => x.CodeGiamGia == model.CodeGiamGia))
            {
                TempData["PromotionError"] = "Mã khuyến mãi đã tồn tại.";
                return RedirectToAction(nameof(Index));
            }

            model.MaGiamGia = await GeneratePromotionIdAsync();
            model.SoLuongDaDung = 0;
            _context.MaGiamGia.Add(model);
            await _context.SaveChangesAsync();
            TempData["PromotionSuccess"] = "Đã tạo khuyến mãi mới.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                TempData["PromotionError"] = "Thiếu mã khuyến mãi.";
                return RedirectToAction(nameof(Index));
            }

            var promotion = await _context.MaGiamGia.FindAsync(id.Trim());
            if (promotion == null)
            {
                TempData["PromotionError"] = "Không tìm thấy khuyến mãi.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                _context.MaGiamGia.Remove(promotion);
                await _context.SaveChangesAsync();
                TempData["PromotionSuccess"] = "Đã xóa khuyến mãi.";
            }
            catch (DbUpdateException)
            {
                _context.ChangeTracker.Clear();
                TempData["PromotionError"] = "Không thể xóa khuyến mãi vì đã phát sinh dữ liệu sử dụng.";
            }

            return RedirectToAction(nameof(Index));
        }

        private static AdminPromotionManagementItemViewModel BuildPromotionItem(
            MaGiamGium promotion,
            DateOnly today,
            int usedCount)
        {
            var (statusLabel, statusKey, statusClass, statusIcon) = GetPromotionStatus(promotion, today, usedCount);
            var usagePercent = promotion.SoLuongPhatHanh <= 0
                ? 0
                : Math.Min(100, Math.Round((decimal)usedCount / promotion.SoLuongPhatHanh * 100, 1));

            return new AdminPromotionManagementItemViewModel
            {
                PromotionId = promotion.MaGiamGia.Trim(),
                Code = promotion.CodeGiamGia.Trim(),
                Name = promotion.TenMaGiamGia.Trim(),
                Description = string.IsNullOrWhiteSpace(promotion.MoTa)
                    ? "Chưa có mô tả cho chương trình này."
                    : promotion.MoTa.Trim(),
                DiscountLabel = BuildDiscountLabel(promotion),
                MaxDiscountLabel = promotion.GiamToiDa.HasValue
                    ? FormatCurrency(promotion.GiamToiDa.Value)
                    : "Không giới hạn",
                MinimumInvoiceLabel = FormatCurrency(promotion.HoaDonToiThieu),
                ScopeLabel = string.IsNullOrWhiteSpace(promotion.PhamViApDung)
                    ? "Tất cả"
                    : promotion.PhamViApDung.Trim(),
                DateRangeLabel = $"{FormatDate(promotion.TuNgay)} - {FormatDate(promotion.DenNgay)}",
                StartDateValue = promotion.TuNgay.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                EndDateValue = promotion.DenNgay.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                IssuedCount = promotion.SoLuongPhatHanh,
                UsedCount = usedCount,
                UsagePercent = usagePercent,
                StatusLabel = statusLabel,
                StatusKey = statusKey,
                StatusClass = statusClass,
                StatusIcon = statusIcon
            };
        }

        private static (string Label, string Key, string ClassName, string Icon) GetPromotionStatus(
            MaGiamGium promotion,
            DateOnly today,
            int usedCount)
        {
            if (!string.IsNullOrWhiteSpace(promotion.TrangThai) &&
                (promotion.TrangThai.Contains("ngừng", StringComparison.OrdinalIgnoreCase) ||
                 promotion.TrangThai.Contains("hủy", StringComparison.OrdinalIgnoreCase)))
            {
                return ("Ngừng áp dụng", "inactive", "inactive", "bi-pause-circle");
            }

            if (today < promotion.TuNgay)
            {
                return ("Sắp diễn ra", "upcoming", "upcoming", "bi-clock");
            }

            if (today > promotion.DenNgay)
            {
                return ("Đã hết hạn", "expired", "expired", "bi-x-circle");
            }

            if (promotion.SoLuongPhatHanh > 0 && usedCount >= promotion.SoLuongPhatHanh)
            {
                return ("Hết lượt", "inactive", "inactive", "bi-dash-circle");
            }

            return ("Đang áp dụng", "active", "active", "bi-check-circle");
        }

        private static void NormalizePromotion(MaGiamGium model)
        {
            model.MaGiamGia = model.MaGiamGia?.Trim() ?? string.Empty;
            model.CodeGiamGia = (model.CodeGiamGia?.Trim() ?? string.Empty).ToUpperInvariant();
            model.TenMaGiamGia = model.TenMaGiamGia?.Trim() ?? string.Empty;
            model.LoaiGiamGia = model.LoaiGiamGia?.Trim() ?? DomainValues.MaGiamGiaLoai.PhanTram;
            model.PhamViApDung = string.IsNullOrWhiteSpace(model.PhamViApDung) ? "Tất cả" : model.PhamViApDung.Trim();
            model.TrangThai = string.IsNullOrWhiteSpace(model.TrangThai) ? "Hoạt động" : model.TrangThai.Trim();
            model.MoTa = model.MoTa?.Trim();
            model.GhiChu = model.GhiChu?.Trim();
        }

        private static bool ValidatePromotion(MaGiamGium model, out string message)
        {
            if (string.IsNullOrWhiteSpace(model.CodeGiamGia) || string.IsNullOrWhiteSpace(model.TenMaGiamGia))
            {
                message = "Vui lòng nhập tên chương trình và mã khuyến mãi.";
                return false;
            }

            if (model.GiaTriGiam <= 0)
            {
                message = "Giá trị giảm phải lớn hơn 0.";
                return false;
            }

            if (model.LoaiGiamGia == DomainValues.MaGiamGiaLoai.PhanTram && model.GiaTriGiam > 100)
            {
                message = "Phần trăm giảm không được lớn hơn 100.";
                return false;
            }

            if (model.HoaDonToiThieu < 0 || model.SoLuongPhatHanh <= 0)
            {
                message = "Điều kiện áp dụng không hợp lệ.";
                return false;
            }

            if (model.TuNgay > model.DenNgay)
            {
                message = "Ngày bắt đầu không được lớn hơn ngày kết thúc.";
                return false;
            }

            message = string.Empty;
            return true;
        }

        private async Task<string> GeneratePromotionIdAsync()
        {
            var ids = await _context.MaGiamGia
                .AsNoTracking()
                .Select(x => x.MaGiamGia)
                .ToListAsync();
            var nextNumber = ids
                .Select(id => Regex.Match(id.Trim(), @"^MG(\d+)$", RegexOptions.IgnoreCase))
                .Where(match => match.Success)
                .Select(match => int.Parse(match.Groups[1].Value))
                .DefaultIfEmpty(0)
                .Max() + 1;

            return $"MG{nextNumber:0000}";
        }

        private static string BuildDiscountLabel(MaGiamGium promotion)
        {
            return promotion.LoaiGiamGia == DomainValues.MaGiamGiaLoai.PhanTram
                ? $"{promotion.GiaTriGiam:0.##}%"
                : FormatCurrency(promotion.GiaTriGiam);
        }

        private static string FormatDate(DateOnly date)
        {
            return date.ToString("dd/MM/yyyy", CultureInfo.GetCultureInfo("vi-VN"));
        }

        private static string FormatCurrency(decimal value)
        {
            return $"{value.ToString("N0", CultureInfo.GetCultureInfo("vi-VN"))} đ";
        }
    }
}
