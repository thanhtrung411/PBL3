using Microsoft.EntityFrameworkCore;
using PBL3.Data;
using PBL3.Models;
using PBL3.Services.Interfaces;

namespace PBL3.Services
{
    public class KhachHangService : IKhachHangService
    {
        private readonly ApplicationDbContext _context;

        public KhachHangService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<KhachHang>> GetAllAsync()
        {
            return await _context.KhachHangs
                .OrderBy(x => x.MaKh)
                .ToListAsync();
        }

        public async Task<KhachHang?> GetByIdAsync(string maKh)
        {
            return await _context.KhachHangs
                .FirstOrDefaultAsync(x => x.MaKh == maKh);
        }

        public async Task<bool> KiemTraTrungMaAsync(string maKh)
        {
            return await _context.KhachHangs
                .AnyAsync(x => x.MaKh == maKh);
        }

        public async Task<bool> KiemTraTrungCccdAsync(string cccd, string? maKhBoQua = null)
        {
            if (string.IsNullOrWhiteSpace(cccd)) return false;

            if (string.IsNullOrWhiteSpace(maKhBoQua))
            {
                return await _context.KhachHangs
                    .AnyAsync(x => x.Cccd == cccd);
            }
            return await _context.KhachHangs
                .AnyAsync(x => x.Cccd == cccd && x.MaKh != maKhBoQua);
        }

        public async Task<bool> CreateAsync(KhachHang khachHang)
        {
            khachHang.MaKh = khachHang.MaKh.Trim();
            khachHang.HoTen = khachHang.HoTen.Trim();
            khachHang.GioiTinh = khachHang.GioiTinh?.Trim();
            khachHang.Cccd = khachHang.Cccd?.Trim();
            khachHang.SoDienThoai = khachHang.SoDienThoai?.Trim();
            khachHang.Email = khachHang.Email?.Trim();
            khachHang.DiaChi = khachHang.DiaChi?.Trim();
            khachHang.QuocTich = khachHang.QuocTich?.Trim();

            if (await KiemTraTrungMaAsync(khachHang.MaKh))
            {
                return false;
            }

            if (!string.IsNullOrWhiteSpace(khachHang.Cccd) && await KiemTraTrungCccdAsync(khachHang.Cccd))
            {
                return false;
            }

            _context.KhachHangs.Add(khachHang);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> UpdateAsync(KhachHang khachHang)
        {
            khachHang.MaKh = khachHang.MaKh.Trim();
            khachHang.HoTen = khachHang.HoTen.Trim();
            khachHang.GioiTinh = khachHang.GioiTinh?.Trim();
            khachHang.Cccd = khachHang.Cccd?.Trim();
            khachHang.SoDienThoai = khachHang.SoDienThoai?.Trim();
            khachHang.Email = khachHang.Email?.Trim();
            khachHang.DiaChi = khachHang.DiaChi?.Trim();
            khachHang.QuocTich = khachHang.QuocTich?.Trim();

            if (!string.IsNullOrWhiteSpace(khachHang.Cccd) && await KiemTraTrungCccdAsync(khachHang.Cccd, khachHang.MaKh))
            {
                return false;
            }

            _context.KhachHangs.Update(khachHang);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteAsync(string maKh)
        {
            var khachHang = await _context.KhachHangs.FindAsync(maKh);
            if (khachHang == null)
            {
                return false;
            }

            try
            {
                _context.KhachHangs.Remove(khachHang);
                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<KhachHang?> GetByCccdAsync(string cccd)
        {
            return await _context.KhachHangs.FirstOrDefaultAsync(x => x.Cccd == cccd);
        }
    }
}
