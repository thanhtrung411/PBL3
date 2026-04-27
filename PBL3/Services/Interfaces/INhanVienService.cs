using PBL3.Models;

namespace PBL3.Services.Interfaces
{
    public interface INhanVienService
    {
        Task<List<NhanVien>> GetAllAsync();
        Task<NhanVien?> GetByIdAsync(string maNv);
        Task<bool> KiemTraTrungMaAsync(string maNv);
        Task<bool> KiemTraTrungSoDienThoaiAsync(string soDienThoai, string? maNvBoQua = null);
        Task<bool> KiemTraTrungEmailAsync(string email, string? maNvBoQua = null);
        Task<bool> CreateAsync(NhanVien nhanVien);
        Task<bool> UpdateAsync(NhanVien nhanVien);
        Task<bool> DeleteAsync(string maNv);
    }
}
