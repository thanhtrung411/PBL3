using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace PBL3.Models;

[Table("NhanVien")]
[Index("Email", Name = "UQ_NhanVien_Email", IsUnique = true)]
[Index("SoDienThoai", Name = "UQ_NhanVien_SDT", IsUnique = true)]
public partial class NhanVien
{
    [Key]
    [Column("MaNV")]
    [StringLength(10)]
    [Unicode(false)]
    public string MaNv { get; set; } = null!;

    [StringLength(100)]
    public string HoTen { get; set; } = null!;

    [StringLength(10)]
    public string? GioiTinh { get; set; }

    public DateOnly? NgaySinh { get; set; }

    [StringLength(15)]
    [Unicode(false)]
    public string? SoDienThoai { get; set; }

    [StringLength(100)]
    [Unicode(false)]
    public string? Email { get; set; }

    [StringLength(255)]
    public string? DiaChi { get; set; }

    [StringLength(50)]
    public string? ChucVu { get; set; }

    [StringLength(30)]
    public string TrangThai { get; set; } = null!;

    [InverseProperty("MaNvNavigation")]
    public virtual ICollection<DatPhong> DatPhongs { get; set; } = new List<DatPhong>();

    [InverseProperty("MaNvNavigation")]
    public virtual TaiKhoan? TaiKhoan { get; set; }
}
