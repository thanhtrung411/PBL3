using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace PBL3.Models;

[Table("KhachHang")]
[Index("Cccd", Name = "UQ_KhachHang_CCCD", IsUnique = true)]
public partial class KhachHang
{
    [Key]
    [Column("MaKH")]
    [StringLength(10)]
    [Unicode(false)]
    public string MaKh { get; set; } = null!;

    [StringLength(100)]
    public string HoTen { get; set; } = null!;

    [StringLength(10)]
    public string? GioiTinh { get; set; }

    public DateOnly? NgaySinh { get; set; }

    [Column("CCCD")]
    [StringLength(20)]
    [Unicode(false)]
    public string? Cccd { get; set; }

    [StringLength(15)]
    [Unicode(false)]
    public string? SoDienThoai { get; set; }

    [StringLength(100)]
    [Unicode(false)]
    public string? Email { get; set; }

    [StringLength(255)]
    public string? DiaChi { get; set; }

    [StringLength(50)]
    public string? QuocTich { get; set; }

    [InverseProperty("MaKhNavigation")]
    public virtual ICollection<DatPhong> DatPhongs { get; set; } = new List<DatPhong>();
}
