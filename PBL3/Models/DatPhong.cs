using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace PBL3.Models;

[Table("DatPhong")]
[Index("MaKh", Name = "IX_DatPhong_MaKH")]
[Index("MaNv", Name = "IX_DatPhong_MaNV")]
[Index("NgayDat", Name = "IX_DatPhong_NgayDat")]
[Index("NgayNhanPhong", Name = "IX_DatPhong_NgayNhanPhong")]
[Index("TrangThai", Name = "IX_DatPhong_TrangThai")]
[Index("TrangThai", "NgayDat", Name = "IX_DatPhong_TrangThai_NgayDat")]
[Index("TrangThai", "NgayNhanPhong", "NgayTraPhong", Name = "IX_DatPhong_TrangThai_NgayNhan_NgayTra")]
public partial class DatPhong
{
    [Key]
    [StringLength(10)]
    [Unicode(false)]
    public string MaDatPhong { get; set; } = null!;

    [Column("MaKH")]
    [StringLength(10)]
    [Unicode(false)]
    public string MaKh { get; set; } = null!;

    [Column("MaNV")]
    [StringLength(10)]
    [Unicode(false)]
    public string MaNv { get; set; } = null!;

    // Snapshot thông tin khách hàng tại thời điểm đặt
    [Column("TenKH_Snapshot")]
    [StringLength(100)]
    public string TenKhSnapshot { get; set; } = null!;

    [Column("CCCD_Snapshot")]
    [StringLength(20)]
    [Unicode(false)]
    public string? CccdSnapshot { get; set; }

    [Column("SDT_Snapshot")]
    [StringLength(15)]
    [Unicode(false)]
    public string? SdtSnapshot { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime NgayDat { get; set; }

    public DateOnly NgayNhanPhong { get; set; }

    public DateOnly NgayTraPhong { get; set; }

    [StringLength(30)]
    public string TrangThai { get; set; } = null!;

    [StringLength(255)]
    public string? GhiChu { get; set; }

    // Navigation properties
    [ForeignKey("MaKh")]
    [InverseProperty("DatPhongs")]
    public virtual KhachHang MaKhNavigation { get; set; } = null!;

    [ForeignKey("MaNv")]
    [InverseProperty("DatPhongs")]
    public virtual NhanVien MaNvNavigation { get; set; } = null!;

    // 1:1 với HoaDon
    [InverseProperty("MaDatPhongNavigation")]
    public virtual HoaDon? HoaDon { get; set; }
}
