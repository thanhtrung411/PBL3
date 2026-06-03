using Microsoft.EntityFrameworkCore;
using PBL3.Data;
using PBL3.Models;
using PBL3.Services.Interfaces;

namespace PBL3.Services;

public class InvoicePromotionService : IInvoicePromotionService
{
    private readonly ApplicationDbContext _context;

    public InvoicePromotionService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<MaGiamGium?> ApplyBestPromotionAsync(
        HoaDon invoice,
        CancellationToken cancellationToken = default,
        bool serviceOnly = false)
    {
        var existingPromotionId = invoice.MaGiamGiaPhong?.Trim();
        var existingDiscount = Math.Max(invoice.TienGiamGiaPhong, 0);
        var subtotal = Math.Max(invoice.TongTienPhong + invoice.TongTienDichVu, 0);
        invoice.MaGiamGiaPhong = null;
        invoice.TienGiamGiaPhong = 0;
        invoice.TongThanhToan = subtotal;

        if (subtotal <= 0)
        {
            return null;
        }

        var today = DateOnly.FromDateTime(DateTime.Today);
        var promotions = await _context.MaGiamGia
            .AsNoTracking()
            .Where(x => x.TuNgay <= today &&
                        x.DenNgay >= today &&
                        x.HoaDonToiThieu <= subtotal)
            .ToListAsync(cancellationToken);

        var usageCounts = await GetPromotionUsageCountsAsync(invoice.MaHoaDon, cancellationToken);
        var best = promotions
            .Where(IsActive)
            .Select(x => new
            {
                Promotion = x,
                BaseAmount = GetPromotionBaseAmount(x, invoice, serviceOnly),
                IsExisting = string.Equals(x.MaGiamGia.Trim(), existingPromotionId, StringComparison.OrdinalIgnoreCase)
            })
            .Where(x => x.BaseAmount >= x.Promotion.HoaDonToiThieu)
            .Where(x => x.IsExisting || HasAvailableUsage(x.Promotion, usageCounts))
            .Select(x => new
            {
                x.Promotion,
                x.IsExisting,
                Discount = CalculateDiscount(x.Promotion, x.BaseAmount)
            })
            .Where(x => x.Discount > 0)
            .OrderByDescending(x => x.Discount)
            .ThenByDescending(x => x.IsExisting)
            .ThenBy(x => x.Promotion.DenNgay)
            .FirstOrDefault();

        if (serviceOnly &&
            !string.IsNullOrWhiteSpace(existingPromotionId) &&
            existingDiscount > 0)
        {
            var existingPromotion = promotions.FirstOrDefault(x =>
                string.Equals(x.MaGiamGia.Trim(), existingPromotionId, StringComparison.OrdinalIgnoreCase));
            if (existingPromotion == null)
            {
                existingPromotion = await _context.MaGiamGia
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x => x.MaGiamGia == existingPromotionId, cancellationToken);
            }

            var retainedDiscount = Math.Min(existingDiscount, subtotal);
            if (existingPromotion != null &&
                retainedDiscount > 0 &&
                (best == null || retainedDiscount >= best.Discount))
            {
                invoice.MaGiamGiaPhong = existingPromotion.MaGiamGia;
                invoice.TienGiamGiaPhong = retainedDiscount;
                invoice.TongThanhToan = Math.Max(subtotal - retainedDiscount, 0);

                return existingPromotion;
            }
        }

        if (best == null)
        {
            return null;
        }

        invoice.MaGiamGiaPhong = best.Promotion.MaGiamGia;
        invoice.TienGiamGiaPhong = best.Discount;
        invoice.TongThanhToan = Math.Max(subtotal - best.Discount, 0);
        invoice.GhiChu = AppendPromotionNote(invoice.GhiChu, best.Promotion);

        return best.Promotion;
    }

    private async Task<Dictionary<string, int>> GetPromotionUsageCountsAsync(
        string? currentInvoiceId,
        CancellationToken cancellationToken)
    {
        var invoiceId = string.IsNullOrWhiteSpace(currentInvoiceId) ||
                        string.Equals(currentInvoiceId.Trim(), "PREVIEW", StringComparison.OrdinalIgnoreCase)
            ? null
            : currentInvoiceId.Trim();

        var query = _context.HoaDons
            .AsNoTracking()
            .Where(x => x.MaGiamGiaPhong != null &&
                        x.TienGiamGiaPhong > 0 &&
                        x.TrangThai != DomainValues.HoaDonTrangThai.DaHuy);

        if (!string.IsNullOrWhiteSpace(invoiceId))
        {
            query = query.Where(x => x.MaHoaDon != invoiceId);
        }

        var rows = await query
            .Select(x => x.MaGiamGiaPhong!)
            .ToListAsync(cancellationToken);

        return rows
            .GroupBy(x => x.Trim(), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(x => x.Key, x => x.Count(), StringComparer.OrdinalIgnoreCase);
    }

    private static bool HasAvailableUsage(
        MaGiamGium promotion,
        IReadOnlyDictionary<string, int> usageCounts)
    {
        if (promotion.SoLuongPhatHanh <= 0)
        {
            return true;
        }

        usageCounts.TryGetValue(promotion.MaGiamGia.Trim(), out var usedCount);
        return usedCount < promotion.SoLuongPhatHanh;
    }

    private static bool IsActive(MaGiamGium promotion)
    {
        if (string.IsNullOrWhiteSpace(promotion.TrangThai))
        {
            return true;
        }

        var status = RemoveVietnameseMarks(promotion.TrangThai).ToLowerInvariant();
        return !status.Contains("ngung") &&
               !status.Contains("huy") &&
               !status.Contains("inactive") &&
               !status.Contains("tam dung");
    }

    private static decimal CalculateDiscount(MaGiamGium promotion, decimal subtotal)
    {
        var type = promotion.LoaiGiamGia?.Trim();
        var discount = string.Equals(type, DomainValues.MaGiamGiaLoai.PhanTram, StringComparison.OrdinalIgnoreCase)
            ? subtotal * promotion.GiaTriGiam / 100m
            : promotion.GiaTriGiam;

        if (promotion.GiamToiDa.HasValue && promotion.GiamToiDa.Value > 0)
        {
            discount = Math.Min(discount, promotion.GiamToiDa.Value);
        }

        return Math.Min(Math.Max(discount, 0), subtotal);
    }

    private static decimal GetPromotionBaseAmount(
        MaGiamGium promotion,
        HoaDon invoice,
        bool serviceOnly)
    {
        var scope = GetScopeKey(promotion.PhamViApDung);
        var serviceAmount = Math.Max(invoice.TongTienDichVu, 0);
        var roomAmount = Math.Max(invoice.TongTienPhong, 0);

        if (serviceOnly)
        {
            return scope == DomainValues.MaGiamGiaPhamVi.ChiPhong
                ? 0
                : serviceAmount;
        }

        return scope switch
        {
            DomainValues.MaGiamGiaPhamVi.ChiPhong => roomAmount,
            DomainValues.MaGiamGiaPhamVi.ChiDichVu => serviceAmount,
            _ => roomAmount + serviceAmount
        };
    }

    private static string GetScopeKey(string? scope)
    {
        var normalized = NormalizeScope(scope);

        if (normalized.Contains("dichvu") || normalized.Contains("service"))
        {
            return DomainValues.MaGiamGiaPhamVi.ChiDichVu;
        }

        if (normalized.Contains("phong") || normalized.Contains("room"))
        {
            return DomainValues.MaGiamGiaPhamVi.ChiPhong;
        }

        return DomainValues.MaGiamGiaPhamVi.TatCa;
    }

    private static string NormalizeScope(string? scope)
    {
        if (string.IsNullOrWhiteSpace(scope))
        {
            return string.Empty;
        }

        var normalized = RemoveVietnameseMarks(scope).ToLowerInvariant();
        return new string(normalized.Where(char.IsLetterOrDigit).ToArray());
    }

    private static string? AppendPromotionNote(string? note, MaGiamGium promotion)
    {
        var code = promotion.CodeGiamGia.Trim();
        var marker = $"Khuyến mãi tự động: {code}";
        if (!string.IsNullOrWhiteSpace(note) &&
            note.Contains(marker, StringComparison.OrdinalIgnoreCase))
        {
            return note;
        }

        return string.IsNullOrWhiteSpace(note)
            ? marker
            : $"{note.Trim()} | {marker}";
    }

    private static string RemoveVietnameseMarks(string value)
    {
        var normalized = value.Normalize(System.Text.NormalizationForm.FormD);
        var chars = normalized
            .Where(c => System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c) !=
                        System.Globalization.UnicodeCategory.NonSpacingMark)
            .ToArray();
        return new string(chars).Normalize(System.Text.NormalizationForm.FormC)
            .Replace('đ', 'd')
            .Replace('Đ', 'D');
    }
}
