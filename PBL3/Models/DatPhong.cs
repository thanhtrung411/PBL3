using System;
using System.Collections.Generic;

namespace PBL3.Models;

public partial class DatPhong
{
    public string MaDatPhong { get; set; } = null!;

    public string MaKh { get; set; } = null!;

    public string MaNv { get; set; } = null!;

    public DateTime? NgayDat { get; set; }

    public DateOnly NgayNhanPhong { get; set; }

    public DateOnly NgayTraPhong { get; set; }

    public int? SoNguoi { get; set; }

    public string? TrangThai { get; set; }

    public string? GhiChu { get; set; }

    public virtual ICollection<ChiTietDatPhong> ChiTietDatPhongs { get; set; } = new List<ChiTietDatPhong>();

    public virtual ICollection<HoaDon> HoaDons { get; set; } = new List<HoaDon>();

    public virtual KhachHang MaKhNavigation { get; set; } = null!;

    public virtual NhanVien MaNvNavigation { get; set; } = null!;
}
