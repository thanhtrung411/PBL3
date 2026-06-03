using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace PBL3.Models;

[Index("CodeGiamGia", Name = "UQ_MaGiamGia_Code", IsUnique = true)]
[Index("TuNgay", "DenNgay", "HoaDonToiThieu", "TrangThai", Name = "IX_MaGiamGia_ActiveLookup")]
public partial class MaGiamGium
{
    [Key]
    [StringLength(10)]
    [Unicode(false)]
    public string MaGiamGia { get; set; } = null!;

    [StringLength(30)]
    [Unicode(false)]
    public string CodeGiamGia { get; set; } = null!;

    [StringLength(100)]
    public string TenMaGiamGia { get; set; } = null!;

    [StringLength(20)]
    public string LoaiGiamGia { get; set; } = null!;

    [Column(TypeName = "decimal(18, 2)")]
    public decimal GiaTriGiam { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal HoaDonToiThieu { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal? GiamToiDa { get; set; }

    [StringLength(20)]
    public string PhamViApDung { get; set; } = null!;

    public DateOnly TuNgay { get; set; }

    public DateOnly DenNgay { get; set; }

    public int SoLuongPhatHanh { get; set; }

    public int SoLuongDaDung { get; set; }

    [StringLength(30)]
    public string TrangThai { get; set; } = null!;

    [StringLength(255)]
    public string? MoTa { get; set; }

    [StringLength(255)]
    public string? GhiChu { get; set; }

    [InverseProperty("MaGiamGiaNavigation")]
    public virtual ICollection<ChiTietHoaDon> ChiTietHoaDons { get; set; } = new List<ChiTietHoaDon>();

    [InverseProperty("MaGiamGiaPhongNavigation")]
    public virtual ICollection<HoaDon> HoaDons { get; set; } = new List<HoaDon>();
}
