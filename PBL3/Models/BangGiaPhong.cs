using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace PBL3.Models;

[Table("BangGiaPhong")]
[Index("MaLoaiPhong", Name = "IX_BangGiaPhong_MaLoaiPhong")]
[Index("TuNgay", "DenNgay", Name = "IX_BangGiaPhong_TuNgay_DenNgay")]
public partial class BangGiaPhong
{
    [Key]
    [StringLength(10)]
    [Unicode(false)]
    public string MaBangGia { get; set; } = null!;

    [StringLength(10)]
    [Unicode(false)]
    public string MaLoaiPhong { get; set; } = null!;

    public DateOnly TuNgay { get; set; }

    public DateOnly DenNgay { get; set; }

    public byte? ThuApDung { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal GiaApDung { get; set; }

    [StringLength(20)]
    public string LoaiGia { get; set; } = null!;

    public int UuTien { get; set; }

    [StringLength(30)]
    public string TrangThai { get; set; } = null!;

    [StringLength(255)]
    public string? GhiChu { get; set; }

    [ForeignKey("MaLoaiPhong")]
    [InverseProperty("BangGiaPhongs")]
    public virtual LoaiPhong MaLoaiPhongNavigation { get; set; } = null!;
}
