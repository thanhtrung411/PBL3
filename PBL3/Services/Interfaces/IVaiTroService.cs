using PBL3.Models;

namespace PBL3.Services.Interfaces
{
    public interface IVaiTroService
    {
        Task<List<VaiTro>> GetAllAsync();
        Task<VaiTro?> GetByIdAsync(string maVaiTro);
        Task<bool> KiemTraTrungMaAsync(string maVaiTro);
        Task<bool> KiemTraTrungTenAsync(string tenVaiTro, string? maVaiTroBoQua = null);
        Task<bool> CreateAsync(VaiTro vaiTro);
        Task<bool> UpdateAsync(VaiTro vaiTro);
        Task<bool> DeleteAsync(string maVaiTro);
    }
}
