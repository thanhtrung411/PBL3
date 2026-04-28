using PBL3.Models;

namespace PBL3.Services.Interfaces
{
    public interface ITaiKhoanService
    {
        Task<List<TaiKhoan>> GetAllAsync();
        Task<TaiKhoan?> GetByIdAsync(string maTk);
        Task<bool> KiemTraTrungTenDangNhapAsync(string tenDangNhap, string? maTkBoQua = null);
        Task<bool> KiemTraNVDaCoTaiKhoanAsync(string maNv, string? maTkBoQua = null);
        Task<bool> KiemTraTrungMaAsync(string maTk);
        Task<bool> CreateAsync(TaiKhoan taiKhoan);
        Task<bool> UpdateAsync(TaiKhoan taiKhoan);
        Task<bool> DeleteAsync(string maTk);
    }
}
