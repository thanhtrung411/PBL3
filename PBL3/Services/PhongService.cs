using Microsoft.EntityFrameworkCore;
using PBL3.Data;
using PBL3.Models;
using PBL3.Services.Interfaces;

namespace PBL3.Services
{
    public class PhongService : IPhongService
    {
        private readonly ApplicationDbContext _context;

        public PhongService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<Phong>> GetAllAsync()
        {
            return await _context.Phongs
                .Include(x => x.MaLoaiPhongNavigation)
                .OrderBy(x => x.SoPhong)
                .ToListAsync();
        }

        public async Task<Phong?> GetByIdAsync(string maPhong)
        {
            return await _context.Phongs
                .Include(x => x.MaLoaiPhongNavigation)
                .FirstOrDefaultAsync(x => x.MaPhong == maPhong);
        }

        public async Task<bool> KiemTraTrungMaAsync(string maPhong)
        {
            return await _context.Phongs
                .AnyAsync(x => x.MaPhong == maPhong);
        }

        public async Task<bool> KiemTraTrungSoPhongAsync(string soPhong, string? maPhongBoQua = null)
        {
            if (string.IsNullOrWhiteSpace(maPhongBoQua))
            {
                return await _context.Phongs.AnyAsync(x => x.SoPhong == soPhong);
            }
            return await _context.Phongs.AnyAsync(x => x.SoPhong == soPhong && x.MaPhong != maPhongBoQua);
        }

        public async Task<bool> KiemTraMaLoaiPhongTonTaiAsync(string maLoaiPhong)
        {
            return await _context.LoaiPhongs.AnyAsync(x => x.MaLoaiPhong == maLoaiPhong);
        }

        public async Task<bool> CreateAsync(Phong phong)
        {
            phong.MaPhong = phong.MaPhong.Trim();
            phong.SoPhong = phong.SoPhong.Trim();
            phong.MaLoaiPhong = phong.MaLoaiPhong.Trim();
            phong.TrangThai = phong.TrangThai.Trim();
            phong.GhiChu = phong.GhiChu?.Trim();

            if (await KiemTraTrungMaAsync(phong.MaPhong))
            {
                return false;
            }

            if (await KiemTraTrungSoPhongAsync(phong.SoPhong))
            {
                return false;
            }

            if (!await KiemTraMaLoaiPhongTonTaiAsync(phong.MaLoaiPhong))
            {
                return false;
            }

            _context.Phongs.Add(phong);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> UpdateAsync(Phong phong)
        {
            phong.MaPhong = phong.MaPhong.Trim();
            phong.SoPhong = phong.SoPhong.Trim();
            phong.MaLoaiPhong = phong.MaLoaiPhong.Trim();
            phong.TrangThai = phong.TrangThai.Trim();
            phong.GhiChu = phong.GhiChu?.Trim();

            // Truyền MaPhong để exclude chính nó khi kiểm tra trùng
            if (await KiemTraTrungSoPhongAsync(phong.SoPhong, phong.MaPhong))
            {
                return false;
            }

            if (!await KiemTraMaLoaiPhongTonTaiAsync(phong.MaLoaiPhong))
            {
                return false;
            }

            _context.Phongs.Update(phong);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteAsync(string maPhong)
        {
            var phong = await _context.Phongs.FindAsync(maPhong);
            if (phong == null)
            {
                return false;
            }

            try
            {
                _context.Phongs.Remove(phong);
                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> CapNhatTrangThaiPhongAsync(string maPhong, string trangThaiMoi)
        {
            var phong = await _context.Phongs.FindAsync(maPhong);
            if (phong == null) return false;

            phong.TrangThai = trangThaiMoi;
            _context.Update(phong);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}