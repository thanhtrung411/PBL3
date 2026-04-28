using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace PBL3.Models;

[Table("VaiTro")]
[Index("TenVaiTro", Name = "UQ_VaiTro_TenVaiTro", IsUnique = true)]
public partial class VaiTro
{
    [Key]
    [StringLength(10)]
    [Unicode(false)]
    public string MaVaiTro { get; set; } = null!;

    [StringLength(50)]
    public string TenVaiTro { get; set; } = null!;

    [StringLength(255)]
    public string? MoTa { get; set; }

    [InverseProperty("MaVaiTroNavigation")]
    public virtual ICollection<TaiKhoan> TaiKhoans { get; set; } = new List<TaiKhoan>();
}
