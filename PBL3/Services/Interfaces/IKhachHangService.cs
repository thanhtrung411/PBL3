using PBL3.Models;

namespace PBL3.Services.Interfaces
{
    public interface IKhachHangService
    {
        Task<List<KhachHang>> GetAllAsync();
        Task<KhachHang?> GetByIdAsync(string maKh);
        Task<bool> KiemTraTrungMaAsync(string maKh);
        Task<bool> KiemTraTrungCccdAsync(string cccd, string? maKhBoQua = null);
        Task<bool> CreateAsync(KhachHang khachHang);
        Task<bool> UpdateAsync(KhachHang khachHang);
        Task<bool> DeleteAsync(string maKh);
        Task<KhachHang?> GetByCccdAsync(string cccd);
    }
}
