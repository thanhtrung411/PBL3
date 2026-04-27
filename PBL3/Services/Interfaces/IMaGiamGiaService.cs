using PBL3.Models;

namespace PBL3.Services.Interfaces
{
    public interface IMaGiamGiaService
    {
        Task<List<MaGiamGium>> GetAllAsync();
        Task<MaGiamGium?> GetByIdAsync(string maGiamGia);
        Task<bool> KiemTraTrungMaAsync(string maGiamGia);
        Task<bool> KiemTraTrungCodeAsync(string codeGiamGia, string? maGiamGiaBoQua = null);
        Task<bool> CreateAsync(MaGiamGium maGiamGia);
        Task<bool> UpdateAsync(MaGiamGium maGiamGia);
        Task<bool> DeleteAsync(string maGiamGia);
    }
}
