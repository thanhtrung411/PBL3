using Microsoft.EntityFrameworkCore;
using PBL3.Data;
using PBL3.Models;
using PBL3.Services.Interfaces;

namespace PBL3.Services
{
    public class DatPhongService : IDatPhongService
    {
        private readonly ApplicationDbContext _context;

        public DatPhongService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<DatPhong>> GetAllAsync()
        {
            return await _context.DatPhongs
                .AsNoTracking()
                .Include(d => d.MaKhNavigation)
                .Include(d => d.MaNvNavigation)
                .OrderByDescending(d => d.NgayDat)
                .ToListAsync();
        }

        public async Task<DatPhong?> GetByIdAsync(string maDatPhong)
        {
            return await _context.DatPhongs
                .AsNoTracking()
                .Include(d => d.MaKhNavigation)
                .Include(d => d.MaNvNavigation)
                .FirstOrDefaultAsync(m => m.MaDatPhong == maDatPhong);
        }

        public async Task<bool> KiemTraTrungMaAsync(string maDatPhong)
        {
            return await _context.DatPhongs.AnyAsync(e => e.MaDatPhong == maDatPhong);
        }

        public async Task<bool> KiemTraPhongTrongAsync(string maPhong, DateOnly ngayNhan, DateOnly ngayTra, string? maDatPhongNgoaiLe = null)
        {
            var overlappingCTHDs = await _context.ChiTietHoaDons
                .AsNoTracking()
                .Include(c => c.MaHoaDonNavigation)
                .ThenInclude(h => h.MaDatPhongNavigation)
                .Where(c => c.MaPhong == maPhong && 
                            c.MaHoaDonNavigation.MaDatPhongNavigation.TrangThai != DomainValues.DatPhongTrangThai.DaHuy &&
                            c.MaHoaDonNavigation.MaDatPhongNavigation.TrangThai != DomainValues.DatPhongTrangThai.TraPhong &&
                            c.MaHoaDonNavigation.MaDatPhongNavigation.MaDatPhong != maDatPhongNgoaiLe)
                .ToListAsync();

            foreach (var cthd in overlappingCTHDs)
            {
                var datPhong = cthd.MaHoaDonNavigation.MaDatPhongNavigation;
                if (ngayNhan < datPhong.NgayTraPhong && ngayTra > datPhong.NgayNhanPhong)
                {
                    return false; // Trùng phòng
                }
            }

            return true;
        }

        public async Task<bool> CreateAsync(DatPhong datPhong)
        {
            if (await KiemTraTrungMaAsync(datPhong.MaDatPhong)) return false;

            // Snapshot thông tin khách hàng
            var khachHang = await _context.KhachHangs.FindAsync(datPhong.MaKh);
            if (khachHang != null)
            {
                datPhong.TenKhSnapshot = khachHang.HoTen;
                datPhong.CccdSnapshot = khachHang.Cccd;
                datPhong.SdtSnapshot = khachHang.SoDienThoai;
            }

            datPhong.NgayDat = DateTime.UtcNow;
            if (string.IsNullOrEmpty(datPhong.TrangThai))
            {
                datPhong.TrangThai = DomainValues.DatPhongTrangThai.GiuCho;
            }

            // Tự động sinh mã hóa đơn 10 ký tự.
            var newMaHoaDon = await GenerateUniqueMaHoaDonAsync();
            if (newMaHoaDon == null)
            {
                return false;
            }

            var hoaDon = new HoaDon
            {
                MaHoaDon = newMaHoaDon,
                MaDatPhong = datPhong.MaDatPhong,
                TongTienPhong = 0,
                TongTienDichVu = 0,
                TienDatCoc = 0,
                TienGiamGiaPhong = 0,
                TongThanhToan = 0,
                SoTienDaThanhToan = 0,
                TrangThai = DomainValues.HoaDonTrangThai.ChuaThanhToan
            };

            var ownsTransaction = _context.Database.CurrentTransaction == null;
            var transaction = ownsTransaction ? await _context.Database.BeginTransactionAsync() : null;
            try
            {
                _context.Add(datPhong);
                _context.Add(hoaDon);
                await _context.SaveChangesAsync();
                
                if (ownsTransaction)
                {
                    await transaction!.CommitAsync();
                }

                return true;
            }
            catch (DbUpdateException)
            {
                _context.ChangeTracker.Clear();

                if (ownsTransaction)
                {
                    await transaction!.RollbackAsync();
                }

                return false;
            }
            finally
            {
                if (transaction != null)
                {
                    await transaction.DisposeAsync();
                }
            }
        }

        public async Task<bool> UpdateAsync(DatPhong datPhong)
        {
            _context.Update(datPhong);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteAsync(string maDatPhong)
        {
            var datPhong = await _context.DatPhongs.FindAsync(maDatPhong);
            if (datPhong == null) return false;

            try
            {
                _context.DatPhongs.Remove(datPhong);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (DbUpdateException)
            {
                _context.ChangeTracker.Clear();
                return false;
            }
        }

        private async Task<string?> GenerateUniqueMaHoaDonAsync()
        {
            return await CodeGenerator.GenerateFromSequenceAsync(
                _context,
                "dbo.Seq_HoaDon",
                "H",
                9);
        }
    }
}
