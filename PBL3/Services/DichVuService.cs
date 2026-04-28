using Microsoft.EntityFrameworkCore;
using PBL3.Data;
using PBL3.Models;
using PBL3.Services.Interfaces;

namespace PBL3.Services
{
    public class DichVuService : IDichVuService
    {
        private readonly ApplicationDbContext _context;

        public DichVuService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<DichVu>> GetAllAsync()
        {
            return await _context.DichVus
                .OrderBy(x => x.MaDv)
                .ToListAsync();
        }

        public async Task<DichVu?> GetByIdAsync(string maDv)
        {
            return await _context.DichVus
                .FirstOrDefaultAsync(x => x.MaDv == maDv);
        }

        public async Task<bool> KiemTraTrungMaAsync(string maDv)
        {
            return await _context.DichVus
                .AnyAsync(x => x.MaDv == maDv);
        }

        public async Task<bool> KiemTraTrungTenAsync(string tenDv, string? maDvBoQua = null)
        {
            if (string.IsNullOrWhiteSpace(maDvBoQua))
            {
                return await _context.DichVus
                    .AnyAsync(x => x.TenDv == tenDv);
            }
            return await _context.DichVus
                .AnyAsync(x => x.TenDv == tenDv && x.MaDv != maDvBoQua);
        }

        public async Task<bool> CreateAsync(DichVu dichVu)
        {
            dichVu.MaDv = dichVu.MaDv.Trim();
            dichVu.TenDv = dichVu.TenDv.Trim();
            dichVu.DonViTinh = dichVu.DonViTinh.Trim();
            dichVu.TrangThai = dichVu.TrangThai.Trim();
            dichVu.GhiChu = dichVu.GhiChu?.Trim();

            if (await KiemTraTrungMaAsync(dichVu.MaDv))
            {
                return false;
            }

            if (await KiemTraTrungTenAsync(dichVu.TenDv))
            {
                return false;
            }

            _context.DichVus.Add(dichVu);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> UpdateAsync(DichVu dichVu)
        {
            dichVu.MaDv = dichVu.MaDv.Trim();
            dichVu.TenDv = dichVu.TenDv.Trim();
            dichVu.DonViTinh = dichVu.DonViTinh.Trim();
            dichVu.TrangThai = dichVu.TrangThai.Trim();
            dichVu.GhiChu = dichVu.GhiChu?.Trim();

            if (await KiemTraTrungTenAsync(dichVu.TenDv, dichVu.MaDv))
            {
                return false;
            }

            _context.DichVus.Update(dichVu);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteAsync(string maDv)
        {
            var dichVu = await _context.DichVus.FindAsync(maDv);
            if (dichVu == null)
            {
                return false;
            }

            try
            {
                _context.DichVus.Remove(dichVu);
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
