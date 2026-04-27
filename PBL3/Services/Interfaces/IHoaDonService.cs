using PBL3.Models;

namespace PBL3.Services.Interfaces
{
    public interface IHoaDonService
    {
        Task<List<HoaDon>> GetAllAsync();
        Task<HoaDon?> GetByIdAsync(string maHoaDon);
        Task<bool> KiemTraTrungMaAsync(string maHoaDon);
        Task<bool> CreateAsync(HoaDon hoaDon);
        Task<bool> UpdateAsync(HoaDon hoaDon);
        Task<bool> DeleteAsync(string maHoaDon);
        Task<bool> TinhToanTongTienAsync(string maHoaDon);
    }
}
