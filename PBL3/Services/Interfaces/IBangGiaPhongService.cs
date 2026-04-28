using PBL3.Models;

namespace PBL3.Services.Interfaces
{
    public interface IBangGiaPhongService
    {
        Task<List<BangGiaPhong>> GetAllAsync();
        Task<BangGiaPhong?> GetByIdAsync(string maBangGia);
        Task<bool> KiemTraTrungMaAsync(string maBangGia);
        Task<bool> CreateAsync(BangGiaPhong bangGiaPhong);
        Task<bool> UpdateAsync(BangGiaPhong bangGiaPhong);
        Task<bool> DeleteAsync(string maBangGia);
        Task<decimal> LayGiaPhongHienTaiAsync(string maLoaiPhong);
    }
}
