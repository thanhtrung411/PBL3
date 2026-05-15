using Microsoft.EntityFrameworkCore;
using PBL3.Data;
using PBL3.Models;
using PBL3.Services.Interfaces;

namespace PBL3.Services
{
    public class LinkAnhService : ILinkAnhService
    {
        private readonly ApplicationDbContext _context;

        public LinkAnhService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<LinkAnh>> GetAllAsync()
        {
            return await _context.LinkAnhs
                .AsNoTracking()
                .OrderBy(x => x.DoiTuong)
                .ThenByDescending(x => x.LaAnhDaiDien)
                .ThenBy(x => x.ThuTu)
                .ThenBy(x => x.MaAnh)
                .ToListAsync();
        }

        public async Task<LinkAnh?> GetByIdAsync(string maAnh)
        {
            return await _context.LinkAnhs
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.MaAnh == maAnh);
        }

        public async Task<bool> KiemTraTrungMaAsync(string maAnh)
        {
            return await _context.LinkAnhs.AnyAsync(x => x.MaAnh == maAnh);
        }

        public async Task<bool> CreateAsync(LinkAnh linkAnh)
        {
            Normalize(linkAnh);

            if (await KiemTraTrungMaAsync(linkAnh.MaAnh))
            {
                return false;
            }

            _context.LinkAnhs.Add(linkAnh);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> UpdateAsync(LinkAnh linkAnh)
        {
            Normalize(linkAnh);
            _context.LinkAnhs.Update(linkAnh);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteAsync(string maAnh)
        {
            var linkAnh = await _context.LinkAnhs.FindAsync(maAnh);
            if (linkAnh == null)
            {
                return false;
            }

            try
            {
                _context.LinkAnhs.Remove(linkAnh);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (DbUpdateException)
            {
                _context.ChangeTracker.Clear();
                return false;
            }
        }

        private static void Normalize(LinkAnh linkAnh)
        {
            linkAnh.MaAnh = linkAnh.MaAnh.Trim();
            linkAnh.DoiTuong = linkAnh.DoiTuong.Trim();
            linkAnh.UrlAnh = linkAnh.UrlAnh.Trim();
            linkAnh.ThongTin = linkAnh.ThongTin?.Trim();
            linkAnh.TrangThai = string.IsNullOrWhiteSpace(linkAnh.TrangThai)
                ? DomainValues.LinkAnhTrangThai.HoatDong
                : linkAnh.TrangThai.Trim();

            if (linkAnh.ThuTu <= 0)
            {
                linkAnh.ThuTu = 1;
            }
        }
    }
}
