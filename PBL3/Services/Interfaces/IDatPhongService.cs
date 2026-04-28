using PBL3.Models;

namespace PBL3.Services.Interfaces
{
    public interface IDatPhongService
    {
        Task<List<DatPhong>> GetAllAsync();
        Task<DatPhong?> GetByIdAsync(string maDatPhong);
        Task<bool> KiemTraTrungMaAsync(string maDatPhong);
        Task<bool> CreateAsync(DatPhong datPhong);
        Task<bool> UpdateAsync(DatPhong datPhong);
        Task<bool> DeleteAsync(string maDatPhong);
        Task<bool> KiemTraPhongTrongAsync(string maPhong, DateOnly ngayNhan, DateOnly ngayTra, string? maDatPhongNgoaiLe = null);
    }
}
