using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace PBL3.Models;

[Table("LoaiPhong")]
[Index("TenLoaiPhong", Name = "UQ_LoaiPhong_TenLoaiPhong", IsUnique = true)]
public partial class LoaiPhong
{
    [Key]
    [StringLength(10)]
    [Unicode(false)]
    public string MaLoaiPhong { get; set; } = null!;

    [StringLength(50)]
    public string TenLoaiPhong { get; set; } = null!;

    public int SoNguoiToiDa { get; set; }

    [StringLength(255)]
    public string? MoTa { get; set; }

    [InverseProperty("MaLoaiPhongNavigation")]
    public virtual ICollection<BangGiaPhong> BangGiaPhongs { get; set; } = new List<BangGiaPhong>();

    [InverseProperty("MaLoaiPhongNavigation")]
    public virtual ICollection<Phong> Phongs { get; set; } = new List<Phong>();
}
