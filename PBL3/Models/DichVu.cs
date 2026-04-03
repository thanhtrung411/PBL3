using System;
using System.Collections.Generic;

namespace PBL3.Models;

public partial class DichVu
{
    public string MaDv { get; set; } = null!;

    public string TenDv { get; set; } = null!;

    public decimal DonGia { get; set; }

    public string? DonViTinh { get; set; }

    public string? TrangThai { get; set; }

    public virtual ICollection<ChiTietHoaDon> ChiTietHoaDons { get; set; } = new List<ChiTietHoaDon>();
}
