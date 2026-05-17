using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace PBL3.Models;

[Table("LinkAnh")]
[Index("DoiTuong", Name = "IX_LinkAnh_DoiTuong")]
[Index("DoiTuong", "LaAnhDaiDien", "ThuTu", Name = "IX_LinkAnh_DoiTuong_DaiDien")]
public partial class LinkAnh
{
    [Key]
    [StringLength(10)]
    [Unicode(false)]
    public string MaAnh { get; set; } = null!;

    [StringLength(50)]
    public string DoiTuong { get; set; } = null!;

    [StringLength(500)]
    public string UrlAnh { get; set; } = null!;

    [StringLength(255)]
    public string? ThongTin { get; set; }

    public int ThuTu { get; set; } = 1;

    public bool LaAnhDaiDien { get; set; }

    [StringLength(30)]
    public string TrangThai { get; set; } = DomainValues.LinkAnhTrangThai.HoatDong;
}
