using Microsoft.EntityFrameworkCore;
using PBL3.Data;
using PBL3.Models;
using PBL3.Services.Interfaces;

namespace PBL3.Services
{
    public class LoaiPhongService : ILoaiPhongService
    {
        private readonly ApplicationDbContext _context;

        public LoaiPhongService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<LoaiPhong>> GetAllLoaiPhongsAsync()
        {
            return await _context.LoaiPhongs
                .OrderBy(x => x.MaLoaiPhong)
                .ToListAsync();
        }

        public async Task<LoaiPhong?> GetLoaiPhongByIdAsync(string maLoaiPhong)
        {
            return await _context.LoaiPhongs
                .FirstOrDefaultAsync(x => x.MaLoaiPhong == maLoaiPhong);
        }

        public async Task<bool> KiemTraTrungMaAsync(string maLoaiPhong)
        {
            return await _context.LoaiPhongs
                .AnyAsync(x => x.MaLoaiPhong == maLoaiPhong);
        }

        public async Task<bool> KiemTraTrungTenAsync(string tenLoaiPhong, string? maLoaiPhongBoQua = null)
        {
            if (string.IsNullOrWhiteSpace(maLoaiPhongBoQua))
            {
                return await _context.LoaiPhongs
                    .AnyAsync(x => x.TenLoaiPhong == tenLoaiPhong);
            }
            return await _context.LoaiPhongs
                .AnyAsync(x => x.TenLoaiPhong == tenLoaiPhong && x.MaLoaiPhong != maLoaiPhongBoQua);
        }

        public async Task<bool> CreateLoaiPhongAsync(LoaiPhong loaiPhong)
        {
            loaiPhong.MaLoaiPhong = loaiPhong.MaLoaiPhong.Trim();
            loaiPhong.TenLoaiPhong = loaiPhong.TenLoaiPhong.Trim();
            loaiPhong.MoTa = loaiPhong.MoTa?.Trim();

            if (await KiemTraTrungMaAsync(loaiPhong.MaLoaiPhong))
            {
                return false;
            }

            if (await KiemTraTrungTenAsync(loaiPhong.TenLoaiPhong))
            {
                return false;
            }

            _context.LoaiPhongs.Add(loaiPhong);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> UpdateLoaiPhongAsync(LoaiPhong loaiPhong)
        {
            loaiPhong.MaLoaiPhong = loaiPhong.MaLoaiPhong.Trim();
            loaiPhong.TenLoaiPhong = loaiPhong.TenLoaiPhong.Trim();
            loaiPhong.MoTa = loaiPhong.MoTa?.Trim();

            // Truyền MaLoaiPhong để exclude chính nó khi kiểm tra trùng tên
            if (await KiemTraTrungTenAsync(loaiPhong.TenLoaiPhong, loaiPhong.MaLoaiPhong))
            {
                return false;
            }

            _context.LoaiPhongs.Update(loaiPhong);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteLoaiPhongAsync(string maLoaiPhong)
        {
            var loaiPhong = await _context.LoaiPhongs.FindAsync(maLoaiPhong);
            if (loaiPhong == null)
            {
                return false;
            }

            try
            {
                _context.LoaiPhongs.Remove(loaiPhong);
                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}