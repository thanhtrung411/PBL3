using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace PBL3.Models;

[Table("ChiTietHoaDon")]
[Index("MaDv", Name = "IX_CTHD_MaDV")]
[Index("MaHoaDon", Name = "IX_CTHD_MaHoaDon")]
[Index("MaLoaiPhong", Name = "IX_CTHD_MaLoaiPhong")]
[Index("MaPhong", Name = "IX_CTHD_MaPhong")]
[Index("NgayApDung", Name = "IX_CTHD_NgayApDung")]
public partial class ChiTietHoaDon
{
    [Key]
    [Column("MaCTHD")]
    [StringLength(10)]
    [Unicode(false)]
    public string MaCthd { get; set; } = null!;

    [StringLength(10)]
    [Unicode(false)]
    public string MaHoaDon { get; set; } = null!;

    [StringLength(30)]
    public string LoaiMuc { get; set; } = null!;

    [StringLength(10)]
    [Unicode(false)]
    public string? MaPhong { get; set; }

    [StringLength(10)]
    [Unicode(false)]
    public string? MaLoaiPhong { get; set; }

    [Column("MaDV")]
    [StringLength(10)]
    [Unicode(false)]
    public string? MaDv { get; set; }

    [StringLength(10)]
    [Unicode(false)]
    public string? MaGiamGia { get; set; }

    [StringLength(255)]
    public string NoiDung { get; set; } = null!;

    public DateOnly? NgayApDung { get; set; }

    public int SoNguoi { get; set; }

    public int SoLuong { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal DonGia { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal ThanhTien { get; set; }

    [StringLength(20)]
    public string TrangThai { get; set; } = null!;

    [StringLength(500)]
    public string? GhiChu { get; set; }

    [ForeignKey("MaDv")]
    [InverseProperty("ChiTietHoaDons")]
    public virtual DichVu? MaDvNavigation { get; set; }

    [ForeignKey("MaGiamGia")]
    [InverseProperty("ChiTietHoaDons")]
    public virtual MaGiamGium? MaGiamGiaNavigation { get; set; }

    [ForeignKey("MaHoaDon")]
    [InverseProperty("ChiTietHoaDons")]
    public virtual HoaDon MaHoaDonNavigation { get; set; } = null!;

    [ForeignKey("MaLoaiPhong")]
    [InverseProperty("ChiTietHoaDons")]
    public virtual LoaiPhong? MaLoaiPhongNavigation { get; set; }

    [ForeignKey("MaPhong")]
    [InverseProperty("ChiTietHoaDons")]
    public virtual Phong? MaPhongNavigation { get; set; }
}
