using PBL3.Models;

namespace PBL3.Services.Interfaces
{
    public interface IChiTietHoaDonService
    {
        Task<List<ChiTietHoaDon>> GetAllAsync();
        Task<ChiTietHoaDon?> GetByIdAsync(string maCthd);
        Task<bool> KiemTraTrungMaAsync(string maCthd);
        Task<bool> CreateAsync(ChiTietHoaDon chiTietHoaDon);
        Task<bool> UpdateAsync(ChiTietHoaDon chiTietHoaDon);
        Task<bool> DeleteAsync(string maCthd);
    }
}
