using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace PBL3.Models;

[Table("Phong")]
[Index("SoPhong", Name = "UQ_Phong_SoPhong", IsUnique = true)]
public partial class Phong
{
    [Key]
    [StringLength(10)]
    [Unicode(false)]
    public string MaPhong { get; set; } = null!;

    [StringLength(10)]
    [Unicode(false)]
    public string SoPhong { get; set; } = null!;

    [StringLength(10)]
    [Unicode(false)]
    public string MaLoaiPhong { get; set; } = null!;

    public int Tang { get; set; }

    [StringLength(30)]
    public string TrangThai { get; set; } = null!;

    [StringLength(255)]
    public string? GhiChu { get; set; }

    [InverseProperty("MaPhongNavigation")]
    public virtual ICollection<ChiTietHoaDon> ChiTietHoaDons { get; set; } = new List<ChiTietHoaDon>();

    [ForeignKey("MaLoaiPhong")]
    [InverseProperty("Phongs")]
    public virtual LoaiPhong MaLoaiPhongNavigation { get; set; } = null!;
}
