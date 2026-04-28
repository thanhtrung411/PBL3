using PBL3.Models;
namespace PBL3.Services.Interfaces
{
    public interface ILoaiPhongService
    {
        Task<List<LoaiPhong>> GetAllLoaiPhongsAsync();
        Task<LoaiPhong?> GetLoaiPhongByIdAsync(string id);
        Task<bool> KiemTraTrungMaAsync(string maLoaiPhong);
        Task<bool> KiemTraTrungTenAsync(string tenLoaiPhong, string? maLoaiPhongBoQua = null);
        Task<bool> CreateLoaiPhongAsync(LoaiPhong loaiPhong);
        Task<bool> UpdateLoaiPhongAsync(LoaiPhong loaiPhong);
        Task<bool> DeleteLoaiPhongAsync(string id);
    }
}
