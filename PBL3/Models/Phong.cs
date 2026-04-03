using System;
using System.Collections.Generic;

namespace PBL3.Models;

public partial class Phong
{
    public string MaPhong { get; set; } = null!;

    public string SoPhong { get; set; } = null!;

    public string MaLoaiPhong { get; set; } = null!;

    public int? Tang { get; set; }

    public string? TrangThai { get; set; }

    public string? GhiChu { get; set; }

    public virtual ICollection<ChiTietDatPhong> ChiTietDatPhongs { get; set; } = new List<ChiTietDatPhong>();

    public virtual ICollection<ChiTietHoaDon> ChiTietHoaDons { get; set; } = new List<ChiTietHoaDon>();

    public virtual LoaiPhong MaLoaiPhongNavigation { get; set; } = null!;
}
