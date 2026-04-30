using Microsoft.EntityFrameworkCore;
using PBL3.Data;
using PBL3.Models;
using PBL3.Services.Interfaces;

namespace PBL3.Services
{
    public class HoaDonService : IHoaDonService
    {
        private readonly ApplicationDbContext _context;

        public HoaDonService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<HoaDon>> GetAllAsync()
        {
            return await _context.HoaDons
                .AsNoTracking()
                .Include(h => h.MaDatPhongNavigation)
                .Include(h => h.MaGiamGiaPhongNavigation)
                .OrderBy(h => h.MaHoaDon)
                .ToListAsync();
        }

        public async Task<HoaDon?> GetByIdAsync(string maHoaDon)
        {
            return await _context.HoaDons
                .AsNoTracking()
                .Include(h => h.MaDatPhongNavigation)
                .Include(h => h.MaGiamGiaPhongNavigation)
                .FirstOrDefaultAsync(m => m.MaHoaDon == maHoaDon);
        }

        public async Task<bool> KiemTraTrungMaAsync(string maHoaDon)
        {
            return await _context.HoaDons.AnyAsync(e => e.MaHoaDon == maHoaDon);
        }

        public async Task<bool> CreateAsync(HoaDon hoaDon)
        {
            if (await KiemTraTrungMaAsync(hoaDon.MaHoaDon)) return false;

            _context.Add(hoaDon);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> UpdateAsync(HoaDon hoaDon)
        {
            _context.Update(hoaDon);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteAsync(string maHoaDon)
        {
            var hoaDon = await _context.HoaDons.FindAsync(maHoaDon);
            if (hoaDon == null) return false;

            try
            {
                _context.HoaDons.Remove(hoaDon);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (DbUpdateException)
            {
                _context.ChangeTracker.Clear();
                return false;
            }
        }

        public async Task<bool> TinhToanTongTienAsync(string maHoaDon)
        {
            var hoaDon = await _context.HoaDons
                .Include(h => h.ChiTietHoaDons)
                .Include(h => h.MaGiamGiaPhongNavigation)
                .FirstOrDefaultAsync(h => h.MaHoaDon == maHoaDon);

            if (hoaDon == null) return false;

            hoaDon.TongTienPhong = hoaDon.ChiTietHoaDons
                .Where(c => c.LoaiMuc == DomainValues.ChiTietHoaDonLoaiMuc.Phong)
                .Sum(c => c.ThanhTien);

            hoaDon.TongTienDichVu = hoaDon.ChiTietHoaDons
                .Where(c => c.LoaiMuc == DomainValues.ChiTietHoaDonLoaiMuc.DichVu)
                .Sum(c => c.ThanhTien);

            hoaDon.TienGiamGiaPhong = 0;
            if (hoaDon.MaGiamGiaPhongNavigation != null)
            {
                var discount = hoaDon.MaGiamGiaPhongNavigation;
                var today = DateOnly.FromDateTime(DateTime.Now);
                
                if (discount.TrangThai == "Hoạt động" && 
                    discount.TuNgay <= today && 
                    discount.DenNgay >= today &&
                    (hoaDon.TongTienPhong + hoaDon.TongTienDichVu) >= discount.HoaDonToiThieu)
                {
                    if (discount.LoaiGiamGia == DomainValues.MaGiamGiaLoai.PhanTram)
                    {
                        hoaDon.TienGiamGiaPhong = (hoaDon.TongTienPhong + hoaDon.TongTienDichVu) * (discount.GiaTriGiam / 100m);
                        if (discount.GiamToiDa.HasValue && hoaDon.TienGiamGiaPhong > discount.GiamToiDa.Value)
                        {
                            hoaDon.TienGiamGiaPhong = discount.GiamToiDa.Value;
                        }
                    }
                    else
                    {
                        hoaDon.TienGiamGiaPhong = discount.GiaTriGiam;
                    }
                }
            }

            hoaDon.TongThanhToan = (hoaDon.TongTienPhong + hoaDon.TongTienDichVu) - hoaDon.TienGiamGiaPhong;
            if (hoaDon.TongThanhToan < 0) hoaDon.TongThanhToan = 0;

            await _context.SaveChangesAsync();
            return true;
        }
    }
}
