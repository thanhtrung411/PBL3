using Microsoft.EntityFrameworkCore;
using PBL3.Data;
using PBL3.Models;
using PBL3.Services.Interfaces;

namespace PBL3.Services
{
    public class BangGiaPhongService : IBangGiaPhongService
    {
        private readonly ApplicationDbContext _context;

        public BangGiaPhongService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<BangGiaPhong>> GetAllAsync()
        {
            return await _context.BangGiaPhongs
                .AsNoTracking()
                .Include(b => b.MaLoaiPhongNavigation)
                .OrderBy(b => b.MaBangGia)
                .ToListAsync();
        }

        public async Task<BangGiaPhong?> GetByIdAsync(string maBangGia)
        {
            return await _context.BangGiaPhongs
                .AsNoTracking()
                .Include(b => b.MaLoaiPhongNavigation)
                .FirstOrDefaultAsync(m => m.MaBangGia == maBangGia);
        }

        public async Task<bool> KiemTraTrungMaAsync(string maBangGia)
        {
            return await _context.BangGiaPhongs.AnyAsync(e => e.MaBangGia == maBangGia);
        }

        public async Task<bool> CreateAsync(BangGiaPhong bangGiaPhong)
        {
            if (await KiemTraTrungMaAsync(bangGiaPhong.MaBangGia)) return false;

            _context.Add(bangGiaPhong);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> UpdateAsync(BangGiaPhong bangGiaPhong)
        {
            _context.Update(bangGiaPhong);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteAsync(string maBangGia)
        {
            var bangGiaPhong = await _context.BangGiaPhongs.FindAsync(maBangGia);
            if (bangGiaPhong == null) return false;

            try
            {
                _context.BangGiaPhongs.Remove(bangGiaPhong);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (DbUpdateException)
            {
                _context.ChangeTracker.Clear();
                return false;
            }
        }

        public async Task<decimal> LayGiaPhongHienTaiAsync(string maLoaiPhong)
        {
            var today = DateOnly.FromDateTime(DateTime.Today);
            var todayWeekday = GetVietnamWeekday(DateTime.Today);
            var bangGia = await _context.BangGiaPhongs
                .AsNoTracking()
                .Where(b => b.MaLoaiPhong == maLoaiPhong &&
                            b.TrangThai == "Hoạt động" &&
                            b.GiaApDung > 0 &&
                            ((b.LoaiGia == "MACDINH" && b.ThuApDung == null) ||
                             (b.TuNgay <= today &&
                              b.DenNgay >= today &&
                              ((b.LoaiGia == "THEOTHU" && b.ThuApDung == todayWeekday) ||
                               (b.LoaiGia == "NGAYLE" && b.ThuApDung == null)))))
                .OrderByDescending(b => b.UuTien)
                .ThenByDescending(b => b.TuNgay)
                .FirstOrDefaultAsync();

            if (bangGia != null)
            {
                return bangGia.GiaApDung;
            }

            var fallbackBangGia = await _context.BangGiaPhongs
                .AsNoTracking()
                .Where(b => b.MaLoaiPhong == maLoaiPhong &&
                            b.TrangThai == "Hoạt động" &&
                            b.LoaiGia == "MACDINH" &&
                            b.ThuApDung == null &&
                            b.GiaApDung > 0)
                .OrderByDescending(b => b.DenNgay)
                .ThenByDescending(b => b.TuNgay)
                .FirstOrDefaultAsync();

            return fallbackBangGia?.GiaApDung ?? 0;
        }

        private static byte GetVietnamWeekday(DateTime date)
        {
            return date.DayOfWeek == DayOfWeek.Sunday
                ? (byte)8
                : (byte)((int)date.DayOfWeek + 1);
        }
    }
}
