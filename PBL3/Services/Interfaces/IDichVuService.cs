using PBL3.Models;

namespace PBL3.Services.Interfaces
{
    public interface IDichVuService
    {
        Task<List<DichVu>> GetAllAsync();
        Task<DichVu?> GetByIdAsync(string maDv);
        Task<bool> KiemTraTrungMaAsync(string maDv);
        Task<bool> KiemTraTrungTenAsync(string tenDv, string? maDvBoQua = null);
        Task<bool> CreateAsync(DichVu dichVu);
        Task<bool> UpdateAsync(DichVu dichVu);
        Task<bool> DeleteAsync(string maDv);
    }
}
