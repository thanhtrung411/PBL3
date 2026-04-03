using System;
using System.Collections.Generic;

namespace PBL3.Models;

public partial class LoaiPhong
{
    public string MaLoaiPhong { get; set; } = null!;

    public string TenLoaiPhong { get; set; } = null!;

    public decimal GiaNiemYet { get; set; }

    public int SoNguoiToiDa { get; set; }

    public string? MoTa { get; set; }

    public virtual ICollection<GiaPhongTheoNgay> GiaPhongTheoNgays { get; set; } = new List<GiaPhongTheoNgay>();

    public virtual ICollection<Phong> Phongs { get; set; } = new List<Phong>();
}
