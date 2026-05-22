using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PBL3.Data;
using PBL3.Models;
using PBL3.Services.Interfaces;

namespace PBL3.Services
{
    public class TaiKhoanService : ITaiKhoanService
    {
        private readonly ApplicationDbContext _context;
        private readonly IPasswordHasher<TaiKhoan> _passwordHasher;

        public TaiKhoanService(
            ApplicationDbContext context,
            IPasswordHasher<TaiKhoan> passwordHasher)
        {
            _context = context;
            _passwordHasher = passwordHasher;
        }

        public async Task<List<TaiKhoan>> GetAllAsync()
        {
            return await _context.TaiKhoans
                .AsNoTracking()
                .Include(t => t.MaNvNavigation)
                .Include(t => t.MaVaiTroNavigation)
                .OrderBy(t => t.MaTk)
                .ToListAsync();
        }

        public async Task<TaiKhoan?> GetByIdAsync(string maTk)
        {
            return await _context.TaiKhoans
                .AsNoTracking()
                .Include(t => t.MaNvNavigation)
                .Include(t => t.MaVaiTroNavigation)
                .FirstOrDefaultAsync(m => m.MaTk == maTk);
        }

        public async Task<bool> KiemTraTrungMaAsync(string maTk)
        {
            return await _context.TaiKhoans.AnyAsync(e => e.MaTk == maTk);
        }

        public async Task<bool> KiemTraTrungTenDangNhapAsync(string tenDangNhap, string? maTkBoQua = null)
        {
            if (string.IsNullOrWhiteSpace(maTkBoQua))
            {
                return await _context.TaiKhoans.AnyAsync(x => x.TenDangNhap == tenDangNhap);
            }
            return await _context.TaiKhoans.AnyAsync(x => x.TenDangNhap == tenDangNhap && x.MaTk != maTkBoQua);
        }

        public async Task<bool> KiemTraNVDaCoTaiKhoanAsync(string maNv, string? maTkBoQua = null)
        {
            if (string.IsNullOrWhiteSpace(maTkBoQua))
            {
                return await _context.TaiKhoans.AnyAsync(x => x.MaNv == maNv);
            }
            return await _context.TaiKhoans.AnyAsync(x => x.MaNv == maNv && x.MaTk != maTkBoQua);
        }

        public async Task<bool> CreateAsync(TaiKhoan taiKhoan)
        {
            if (await KiemTraTrungMaAsync(taiKhoan.MaTk)) return false;
            if (await KiemTraTrungTenDangNhapAsync(taiKhoan.TenDangNhap)) return false;
            if (await KiemTraNVDaCoTaiKhoanAsync(taiKhoan.MaNv)) return false;

            EnsurePasswordHash(taiKhoan);
            _context.Add(taiKhoan);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> UpdateAsync(TaiKhoan taiKhoan)
        {
            if (await KiemTraTrungTenDangNhapAsync(taiKhoan.TenDangNhap, taiKhoan.MaTk)) return false;
            if (await KiemTraNVDaCoTaiKhoanAsync(taiKhoan.MaNv, taiKhoan.MaTk)) return false;

            EnsurePasswordHash(taiKhoan);
            _context.Update(taiKhoan);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteAsync(string maTk)
        {
            var taiKhoan = await _context.TaiKhoans.FindAsync(maTk);
            if (taiKhoan == null) return false;

            try
            {
                _context.TaiKhoans.Remove(taiKhoan);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (DbUpdateException)
            {
                _context.ChangeTracker.Clear();
                return false;
            }
        }

        private void EnsurePasswordHash(TaiKhoan taiKhoan)
        {
            if (string.IsNullOrWhiteSpace(taiKhoan.MatKhau) || IsPasswordHash(taiKhoan.MatKhau))
            {
                return;
            }

            taiKhoan.MatKhau = _passwordHasher.HashPassword(taiKhoan, taiKhoan.MatKhau);
        }

        private static bool IsPasswordHash(string password)
        {
            return password.StartsWith("AQAAAA", StringComparison.Ordinal);
        }
    }
}
