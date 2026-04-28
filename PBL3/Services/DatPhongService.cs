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
                .Include(d => d.MaKhNavigation)
                .Include(d => d.MaNvNavigation)
                .OrderByDescending(d => d.NgayDat)
                .ToListAsync();
        }

        public async Task<DatPhong?> GetByIdAsync(string maDatPhong)
        {
            return await _context.DatPhongs
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
                .Include(c => c.MaHoaDonNavigation)
                .ThenInclude(h => h.MaDatPhongNavigation)
                .Where(c => c.MaPhong == maPhong && 
                            c.MaHoaDonNavigation.MaDatPhongNavigation.TrangThai != "Đã hủy" &&
                            c.MaHoaDonNavigation.MaDatPhongNavigation.TrangThai != "Đã trả phòng" &&
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

            datPhong.NgayDat = DateTime.Now;
            if (string.IsNullOrEmpty(datPhong.TrangThai))
            {
                datPhong.TrangThai = "Chờ nhận phòng";
            }

            // Tự động sinh mã hóa đơn 10 ký tự (H + yyMMdd + 3 random digits)
            string newMaHoaDon = "H" + DateTime.Now.ToString("yyMMdd") + new Random().Next(100, 999).ToString();

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
                TrangThai = "Chưa thanh toán"
            };

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                _context.Add(datPhong);
                _context.Add(hoaDon);
                await _context.SaveChangesAsync();
                
                await transaction.CommitAsync();
                return true;
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                return false;
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

            _context.DatPhongs.Remove(datPhong);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
