using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace PBL3.Models;

[Table("HoaDon")]
[Index("MaDatPhong", Name = "UQ_HoaDon_MaDatPhong", IsUnique = true)]
public partial class HoaDon
{
    [Key]
    [StringLength(10)]
    [Unicode(false)]
    public string MaHoaDon { get; set; } = null!;

    [StringLength(10)]
    [Unicode(false)]
    public string MaDatPhong { get; set; } = null!;

    [Column(TypeName = "decimal(18, 2)")]
    public decimal TongTienPhong { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal TongTienDichVu { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal TienDatCoc { get; set; }

    [StringLength(10)]
    [Unicode(false)]
    public string? MaGiamGiaPhong { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal TienGiamGiaPhong { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal TongThanhToan { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal SoTienDaThanhToan { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? NgayThanhToanCuoi { get; set; }

    [StringLength(30)]
    public string? PhuongThucThanhToan { get; set; }

    [StringLength(30)]
    public string TrangThai { get; set; } = null!;

    [StringLength(255)]
    public string? GhiChu { get; set; }

    // Navigation properties
    [InverseProperty("MaHoaDonNavigation")]
    public virtual ICollection<ChiTietHoaDon> ChiTietHoaDons { get; set; } = new List<ChiTietHoaDon>();

    [ForeignKey("MaDatPhong")]
    [InverseProperty("HoaDon")]
    public virtual DatPhong MaDatPhongNavigation { get; set; } = null!;

    [ForeignKey("MaGiamGiaPhong")]
    [InverseProperty("HoaDons")]
    public virtual MaGiamGium? MaGiamGiaPhongNavigation { get; set; }
}
