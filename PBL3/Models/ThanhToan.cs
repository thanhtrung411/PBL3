using System;
using System.Collections.Generic;

namespace PBL3.Models;

public partial class ThanhToan
{
    public string MaThanhToan { get; set; } = null!;

    public string MaHoaDon { get; set; } = null!;

    public DateTime? NgayThanhToan { get; set; }

    public decimal SoTien { get; set; }

    public string? PhuongThuc { get; set; }

    public string? TrangThai { get; set; }

    public virtual HoaDon MaHoaDonNavigation { get; set; } = null!;
}
