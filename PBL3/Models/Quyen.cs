using System;
using System.Collections.Generic;

namespace PBL3.Models;

public partial class Quyen
{
    public string MaQuyen { get; set; } = null!;

    public string TenQuyen { get; set; } = null!;

    public string? MoTa { get; set; }

    public virtual ICollection<VaiTro> MaVaiTros { get; set; } = new List<VaiTro>();
}
