using Microsoft.EntityFrameworkCore;
using PBL3.Data;
using PBL3.Models;
using PBL3.Services.Interfaces;

namespace PBL3.Services
{
    public class MaGiamGiaService : IMaGiamGiaService
    {
        private readonly ApplicationDbContext _context;

        public MaGiamGiaService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<MaGiamGium>> GetAllAsync()
        {
            return await _context.MaGiamGia
                .OrderBy(x => x.MaGiamGia)
                .ToListAsync();
        }

        public async Task<MaGiamGium?> GetByIdAsync(string maGiamGia)
        {
            return await _context.MaGiamGia
                .FirstOrDefaultAsync(x => x.MaGiamGia == maGiamGia);
        }

        public async Task<bool> KiemTraTrungMaAsync(string maGiamGia)
        {
            return await _context.MaGiamGia
                .AnyAsync(x => x.MaGiamGia == maGiamGia);
        }

        public async Task<bool> KiemTraTrungCodeAsync(string codeGiamGia, string? maGiamGiaBoQua = null)
        {
            if (string.IsNullOrWhiteSpace(codeGiamGia)) return false;

            if (string.IsNullOrWhiteSpace(maGiamGiaBoQua))
            {
                return await _context.MaGiamGia
                    .AnyAsync(x => x.CodeGiamGia == codeGiamGia);
            }
            return await _context.MaGiamGia
                .AnyAsync(x => x.CodeGiamGia == codeGiamGia && x.MaGiamGia != maGiamGiaBoQua);
        }

        public async Task<bool> CreateAsync(MaGiamGium maGiamGia)
        {
            maGiamGia.MaGiamGia = maGiamGia.MaGiamGia.Trim();
            maGiamGia.CodeGiamGia = maGiamGia.CodeGiamGia.Trim();
            maGiamGia.TenMaGiamGia = maGiamGia.TenMaGiamGia.Trim();
            
            if (await KiemTraTrungMaAsync(maGiamGia.MaGiamGia))
            {
                return false;
            }

            if (await KiemTraTrungCodeAsync(maGiamGia.CodeGiamGia))
            {
                return false;
            }

            // Mặc định tạo mới thì số lượng đã dùng = 0
            maGiamGia.SoLuongDaDung = 0;

            _context.MaGiamGia.Add(maGiamGia);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> UpdateAsync(MaGiamGium maGiamGia)
        {
            maGiamGia.MaGiamGia = maGiamGia.MaGiamGia.Trim();
            maGiamGia.CodeGiamGia = maGiamGia.CodeGiamGia.Trim();
            maGiamGia.TenMaGiamGia = maGiamGia.TenMaGiamGia.Trim();

            if (await KiemTraTrungCodeAsync(maGiamGia.CodeGiamGia, maGiamGia.MaGiamGia))
            {
                return false;
            }

            _context.MaGiamGia.Update(maGiamGia);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteAsync(string maGiamGia)
        {
            var item = await _context.MaGiamGia.FindAsync(maGiamGia);
            if (item == null)
            {
                return false;
            }

            try
            {
                _context.MaGiamGia.Remove(item);
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
