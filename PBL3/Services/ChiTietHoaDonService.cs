using Microsoft.EntityFrameworkCore;
using PBL3.Data;
using PBL3.Models;
using PBL3.Services.Interfaces;

namespace PBL3.Services
{
    public class ChiTietHoaDonService : IChiTietHoaDonService
    {
        private readonly ApplicationDbContext _context;

        public ChiTietHoaDonService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<ChiTietHoaDon>> GetAllAsync()
        {
            return await _context.ChiTietHoaDons
                .Include(c => c.MaHoaDonNavigation)
                .Include(c => c.MaPhongNavigation)
                .Include(c => c.MaDvNavigation)
                .Include(c => c.MaGiamGiaNavigation)
                .OrderBy(c => c.MaHoaDon).ThenBy(c => c.MaCthd)
                .ToListAsync();
        }

        public async Task<ChiTietHoaDon?> GetByIdAsync(string maCthd)
        {
            return await _context.ChiTietHoaDons
                .Include(c => c.MaHoaDonNavigation)
                .Include(c => c.MaPhongNavigation)
                .Include(c => c.MaDvNavigation)
                .Include(c => c.MaGiamGiaNavigation)
                .FirstOrDefaultAsync(m => m.MaCthd == maCthd);
        }

        public async Task<bool> KiemTraTrungMaAsync(string maCthd)
        {
            return await _context.ChiTietHoaDons.AnyAsync(e => e.MaCthd == maCthd);
        }

        public async Task<bool> CreateAsync(ChiTietHoaDon chiTietHoaDon)
        {
            if (await KiemTraTrungMaAsync(chiTietHoaDon.MaCthd)) return false;

            _context.Add(chiTietHoaDon);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> UpdateAsync(ChiTietHoaDon chiTietHoaDon)
        {
            _context.Update(chiTietHoaDon);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteAsync(string maCthd)
        {
            var cthd = await _context.ChiTietHoaDons.FindAsync(maCthd);
            if (cthd == null) return false;

            _context.ChiTietHoaDons.Remove(cthd);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
