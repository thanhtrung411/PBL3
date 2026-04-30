using Microsoft.EntityFrameworkCore;
using PBL3.Data;
using PBL3.Models;
using PBL3.Services.Interfaces;

namespace PBL3.Services
{
    public class VaiTroService : IVaiTroService
    {
        private readonly ApplicationDbContext _context;

        public VaiTroService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<VaiTro>> GetAllAsync()
        {
            return await _context.VaiTros
                .AsNoTracking()
                .OrderBy(x => x.MaVaiTro)
                .ToListAsync();
        }

        public async Task<VaiTro?> GetByIdAsync(string maVaiTro)
        {
            return await _context.VaiTros
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.MaVaiTro == maVaiTro);
        }

        public async Task<bool> KiemTraTrungMaAsync(string maVaiTro)
        {
            return await _context.VaiTros
                .AnyAsync(x => x.MaVaiTro == maVaiTro);
        }

        public async Task<bool> KiemTraTrungTenAsync(string tenVaiTro, string? maVaiTroBoQua = null)
        {
            if (string.IsNullOrWhiteSpace(maVaiTroBoQua))
            {
                return await _context.VaiTros
                    .AnyAsync(x => x.TenVaiTro == tenVaiTro);
            }
            return await _context.VaiTros
                .AnyAsync(x => x.TenVaiTro == tenVaiTro && x.MaVaiTro != maVaiTroBoQua);
        }

        public async Task<bool> CreateAsync(VaiTro vaiTro)
        {
            vaiTro.MaVaiTro = vaiTro.MaVaiTro.Trim();
            vaiTro.TenVaiTro = vaiTro.TenVaiTro.Trim();
            vaiTro.MoTa = vaiTro.MoTa?.Trim();

            if (await KiemTraTrungMaAsync(vaiTro.MaVaiTro))
            {
                return false;
            }

            if (await KiemTraTrungTenAsync(vaiTro.TenVaiTro))
            {
                return false;
            }

            _context.VaiTros.Add(vaiTro);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> UpdateAsync(VaiTro vaiTro)
        {
            vaiTro.MaVaiTro = vaiTro.MaVaiTro.Trim();
            vaiTro.TenVaiTro = vaiTro.TenVaiTro.Trim();
            vaiTro.MoTa = vaiTro.MoTa?.Trim();

            if (await KiemTraTrungTenAsync(vaiTro.TenVaiTro, vaiTro.MaVaiTro))
            {
                return false;
            }

            _context.VaiTros.Update(vaiTro);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteAsync(string maVaiTro)
        {
            var vaiTro = await _context.VaiTros.FindAsync(maVaiTro);
            if (vaiTro == null)
            {
                return false;
            }

            try
            {
                _context.VaiTros.Remove(vaiTro);
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
