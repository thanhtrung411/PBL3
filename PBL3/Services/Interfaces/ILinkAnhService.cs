using PBL3.Models;

namespace PBL3.Services.Interfaces
{
    public interface ILinkAnhService
    {
        Task<List<LinkAnh>> GetAllAsync();
        Task<LinkAnh?> GetByIdAsync(string maAnh);
        Task<bool> KiemTraTrungMaAsync(string maAnh);
        Task<bool> CreateAsync(LinkAnh linkAnh);
        Task<bool> UpdateAsync(LinkAnh linkAnh);
        Task<bool> DeleteAsync(string maAnh);
    }
}
