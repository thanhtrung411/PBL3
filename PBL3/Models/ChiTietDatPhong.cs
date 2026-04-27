using System;
using System.Collections.Generic;

namespace PBL3.Models;

public partial class ChiTietDatPhong
{
    public string MaDatPhong { get; set; } = null!;

    public string MaPhong { get; set; } = null!;

    public decimal DonGia { get; set; }

    public virtual DatPhong MaDatPhongNavigation { get; set; } = null!;

    public virtual Phong MaPhongNavigation { get; set; } = null!;
}
