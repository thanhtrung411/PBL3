using System;
using System.Collections.Generic;

namespace PBL3.Models;

public partial class ChiTietHoaDon
{
    public string MaCthd { get; set; } = null!;

    public string MaHoaDon { get; set; } = null!;

    public string LoaiMuc { get; set; } = null!;

    public string? MaPhong { get; set; }

    public string? MaDv { get; set; }

    public string NoiDung { get; set; } = null!;

    public int SoLuong { get; set; }

    public decimal DonGia { get; set; }

    public decimal? ThanhTien { get; set; }

    public virtual DichVu? MaDvNavigation { get; set; }

    public virtual HoaDon MaHoaDonNavigation { get; set; } = null!;

    public virtual Phong? MaPhongNavigation { get; set; }
}
