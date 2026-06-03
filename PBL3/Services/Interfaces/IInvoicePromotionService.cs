using PBL3.Models;

namespace PBL3.Services.Interfaces;

public interface IInvoicePromotionService
{
    Task<MaGiamGium?> ApplyBestPromotionAsync(
        HoaDon invoice,
        CancellationToken cancellationToken = default,
        bool serviceOnly = false);
}
