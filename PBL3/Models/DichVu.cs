using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace PBL3.Models;

[Table("DichVu")]
[Index("TenDv", Name = "UQ_DichVu_TenDV", IsUnique = true)]
public partial class DichVu
{
    [Key]
    [Column("MaDV")]
    [StringLength(10)]
    [Unicode(false)]
    public string MaDv { get; set; } = null!;

    [Column("TenDV")]
    [StringLength(100)]
    public string TenDv { get; set; } = null!;

    [Column(TypeName = "decimal(18, 2)")]
    public decimal DonGia { get; set; }

    [StringLength(30)]
    public string DonViTinh { get; set; } = null!;

    [StringLength(30)]
    public string TrangThai { get; set; } = null!;

    [StringLength(255)]
    public string? GhiChu { get; set; }

    [InverseProperty("MaDvNavigation")]
    public virtual ICollection<ChiTietHoaDon> ChiTietHoaDons { get; set; } = new List<ChiTietHoaDon>();
}
