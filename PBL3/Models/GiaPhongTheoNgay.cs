using System;
using System.Collections.Generic;

namespace PBL3.Models;

public partial class GiaPhongTheoNgay
{
    public string MaGia { get; set; } = null!;

    public string MaLoaiPhong { get; set; } = null!;

    public DateOnly TuNgay { get; set; }

    public DateOnly DenNgay { get; set; }

    public int? ThuApDung { get; set; }

    public decimal Gia { get; set; }

    public string? LoaiGia { get; set; }

    public int? UuTien { get; set; }

    public string? GhiChu { get; set; }

    public virtual LoaiPhong MaLoaiPhongNavigation { get; set; } = null!;
}
