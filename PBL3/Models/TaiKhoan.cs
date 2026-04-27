using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace PBL3.Models;

[Table("TaiKhoan")]
[Index("MaNv", Name = "UQ_TaiKhoan_MaNV", IsUnique = true)]
[Index("TenDangNhap", Name = "UQ_TaiKhoan_TenDangNhap", IsUnique = true)]
public partial class TaiKhoan
{
    [Key]
    [Column("MaTK")]
    [StringLength(10)]
    [Unicode(false)]
    public string MaTk { get; set; } = null!;

    [StringLength(50)]
    [Unicode(false)]
    public string TenDangNhap { get; set; } = null!;

    [StringLength(255)]
    public string MatKhau { get; set; } = null!;

    [Column("MaNV")]
    [StringLength(10)]
    [Unicode(false)]
    public string MaNv { get; set; } = null!;

    [StringLength(10)]
    [Unicode(false)]
    public string MaVaiTro { get; set; } = null!;

    [StringLength(30)]
    public string TrangThai { get; set; } = null!;

    [ForeignKey("MaNv")]
    [InverseProperty("TaiKhoan")]
    public virtual NhanVien MaNvNavigation { get; set; } = null!;

    [ForeignKey("MaVaiTro")]
    [InverseProperty("TaiKhoans")]
    public virtual VaiTro MaVaiTroNavigation { get; set; } = null!;
}
