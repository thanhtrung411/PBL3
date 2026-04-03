using System;
using System.Collections.Generic;

namespace PBL3.Models;

public partial class HoaDon
{
    public string MaHoaDon { get; set; } = null!;

    public string MaDatPhong { get; set; } = null!;

    public string MaNv { get; set; } = null!;

    public DateTime? NgayLap { get; set; }

    public decimal? TongTien { get; set; }

    public string? TrangThai { get; set; }

    public string? GhiChu { get; set; }

    public virtual ICollection<ChiTietHoaDon> ChiTietHoaDons { get; set; } = new List<ChiTietHoaDon>();

    public virtual DatPhong MaDatPhongNavigation { get; set; } = null!;

    public virtual NhanVien MaNvNavigation { get; set; } = null!;

    public virtual ICollection<ThanhToan> ThanhToans { get; set; } = new List<ThanhToan>();
}
