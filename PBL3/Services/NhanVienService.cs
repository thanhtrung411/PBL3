using Microsoft.EntityFrameworkCore;
using PBL3.Data;
using PBL3.Models;
using PBL3.Services.Interfaces;

namespace PBL3.Services
{
    public class NhanVienService : INhanVienService
    {
        private readonly ApplicationDbContext _context;

        public NhanVienService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<NhanVien>> GetAllAsync()
        {
            return await _context.NhanViens
                .AsNoTracking()
                .OrderBy(x => x.MaNv)
                .ToListAsync();
        }

        public async Task<NhanVien?> GetByIdAsync(string maNv)
        {
            return await _context.NhanViens
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.MaNv == maNv);
        }

        public async Task<bool> KiemTraTrungMaAsync(string maNv)
        {
            return await _context.NhanViens
                .AnyAsync(x => x.MaNv == maNv);
        }

        public async Task<bool> KiemTraTrungSoDienThoaiAsync(string soDienThoai, string? maNvBoQua = null)
        {
            if (string.IsNullOrWhiteSpace(soDienThoai)) return false;
            
            if (string.IsNullOrWhiteSpace(maNvBoQua))
            {
                return await _context.NhanViens
                    .AnyAsync(x => x.SoDienThoai == soDienThoai);
            }
            return await _context.NhanViens
                .AnyAsync(x => x.SoDienThoai == soDienThoai && x.MaNv != maNvBoQua);
        }

        public async Task<bool> KiemTraTrungEmailAsync(string email, string? maNvBoQua = null)
        {
            if (string.IsNullOrWhiteSpace(email)) return false;

            if (string.IsNullOrWhiteSpace(maNvBoQua))
            {
                return await _context.NhanViens
                    .AnyAsync(x => x.Email == email);
            }
            return await _context.NhanViens
                .AnyAsync(x => x.Email == email && x.MaNv != maNvBoQua);
        }

        public async Task<bool> CreateAsync(NhanVien nhanVien)
        {
            nhanVien.MaNv = nhanVien.MaNv.Trim();
            nhanVien.HoTen = nhanVien.HoTen.Trim();
            nhanVien.GioiTinh = nhanVien.GioiTinh?.Trim();
            nhanVien.SoDienThoai = nhanVien.SoDienThoai?.Trim();
            nhanVien.Email = nhanVien.Email?.Trim();
            nhanVien.DiaChi = nhanVien.DiaChi?.Trim();
            nhanVien.ChucVu = nhanVien.ChucVu?.Trim();
            nhanVien.TrangThai = nhanVien.TrangThai.Trim();

            if (await KiemTraTrungMaAsync(nhanVien.MaNv))
            {
                return false;
            }

            if (!string.IsNullOrWhiteSpace(nhanVien.SoDienThoai) && await KiemTraTrungSoDienThoaiAsync(nhanVien.SoDienThoai))
            {
                return false;
            }

            if (!string.IsNullOrWhiteSpace(nhanVien.Email) && await KiemTraTrungEmailAsync(nhanVien.Email))
            {
                return false;
            }

            _context.NhanViens.Add(nhanVien);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> UpdateAsync(NhanVien nhanVien)
        {
            nhanVien.MaNv = nhanVien.MaNv.Trim();
            nhanVien.HoTen = nhanVien.HoTen.Trim();
            nhanVien.GioiTinh = nhanVien.GioiTinh?.Trim();
            nhanVien.SoDienThoai = nhanVien.SoDienThoai?.Trim();
            nhanVien.Email = nhanVien.Email?.Trim();
            nhanVien.DiaChi = nhanVien.DiaChi?.Trim();
            nhanVien.ChucVu = nhanVien.ChucVu?.Trim();
            nhanVien.TrangThai = nhanVien.TrangThai.Trim();

            if (!string.IsNullOrWhiteSpace(nhanVien.SoDienThoai) && await KiemTraTrungSoDienThoaiAsync(nhanVien.SoDienThoai, nhanVien.MaNv))
            {
                return false;
            }

            if (!string.IsNullOrWhiteSpace(nhanVien.Email) && await KiemTraTrungEmailAsync(nhanVien.Email, nhanVien.MaNv))
            {
                return false;
            }

            _context.NhanViens.Update(nhanVien);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteAsync(string maNv)
        {
            var nhanVien = await _context.NhanViens.FindAsync(maNv);
            if (nhanVien == null)
            {
                return false;
            }

            try
            {
                _context.NhanViens.Remove(nhanVien);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (DbUpdateException)
            {
                _context.ChangeTracker.Clear();
                return false;
            }
        }
    }
}
