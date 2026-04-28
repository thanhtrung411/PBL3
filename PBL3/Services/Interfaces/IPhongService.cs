using PBL3.Models;

namespace PBL3.Services.Interfaces
{
    public interface IPhongService
    {
        Task<List<Phong>> GetAllAsync();
        Task<Phong?> GetByIdAsync(string maPhong);
        Task<bool> KiemTraTrungMaAsync(string maPhong);
        Task<bool> KiemTraTrungSoPhongAsync(string soPhong, string? maPhongBoQua = null);
        Task<bool> KiemTraMaLoaiPhongTonTaiAsync(string maLoaiPhong);
        Task<bool> CreateAsync(Phong phong);
        Task<bool> UpdateAsync(Phong phong);
        Task<bool> DeleteAsync(string maPhong);
        Task<bool> CapNhatTrangThaiPhongAsync(string maPhong, string trangThaiMoi);
    }
}