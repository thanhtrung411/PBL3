using System;
using System.Collections.Generic;

namespace PBL3.Models;

public partial class TaiKhoan
{
    public string MaTk { get; set; } = null!;

    public string TenDangNhap { get; set; } = null!;

    public string MatKhau { get; set; } = null!;

    public string MaNv { get; set; } = null!;

    public string MaVaiTro { get; set; } = null!;

    public string? TrangThai { get; set; }

    public virtual NhanVien MaNvNavigation { get; set; } = null!;

    public virtual VaiTro MaVaiTroNavigation { get; set; } = null!;
}
